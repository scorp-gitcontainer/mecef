using sc.apps.lib.mcf.Const;
using sc.apps.lib.mcf.Service;
using sc.lib.mecef;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Threading;

namespace sc.apps.lib.mcf.Message
{
    /// <summary>
    /// Classe de communication avec le MCF
    /// </summary>
    public class CoreEngine
    {
        #region Variables et constantes
        private EncoderMCF _encoderService = null;
        private DecodeMCF _decoderService = null;
        SerialPort _serialPort;
        #endregion

        #region Constructeur
        /// <summary>
        /// Constructeur permettant d'initialiser les éléments du MCF
        /// </summary>
        /// <param name="encoder">Instance de l'encoder</param>
        /// <param name="decoder">Instance du décodeur</param>
        /// <param name="speed">Rapidité du communication</param>
        /// <param name="portName">Nom du port de communication</param>
        public CoreEngine(EncoderMCF encoder, DecodeMCF decoder, int speed,
                          string portName)
        {
            _encoderService = encoder;
            _decoderService = decoder;
            _serialPort = new SerialPort(portName, speed);
        }
        #endregion

        #region OpenPort
        /// <summary>
        /// Permet d'ouvrir le port de communication avant toutes opération du MCF
        /// </summary>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        public void OpenPort()
        {
            try
            {
                _serialPort.Open();
            }
            catch (UnauthorizedAccessException ex)
            {
                throw ex;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw ex;
            }
            catch (ArgumentException ex)
            {
                throw ex;
            }
            catch (InvalidOperationException ex)
            {
                throw ex;
            }
            catch (System.IO.IOException ex)
            {
                throw ex;
            }
        }
        #endregion

        #region AddNewInvoice

        /// <summary>
        /// Enregistre une facture complète dans le MCF et retour une châine pour générer le QR code
        /// </summary>
        /// <param name="invoiceData">informations de la facture</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="DecoderFallbackException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="System.ServiceProcess.TimeoutException"></exception>
        /// <exception cref="ConfigurationErrorsException"></exception>
        /// <returns></returns>
        public string AddNewInvoice(InvoiceData invoiceData)
        {
            OpenPort();
            //InvoiceDataDetail            
            string _stringVATApplied = string.Empty, _stringPrice = string.Empty, _stringQuantity = string.Empty,
                _stringSpecificTax = string.Empty, _stringVAT = string.Empty, _responseMCF = string.Empty;

            int _newNum = 0;

            string _stringAIB = invoiceData.BuyerAIB == 0 ? "" : invoiceData.BuyerAIB.ToString();
            if (invoiceData != null)
            {
                _stringVAT = string.Format("{0:N}", invoiceData.VAT);
                _stringVAT = _stringVAT.Replace(',', '.');
                try
                {
                    //Création de la facture
                    _newNum++;//= GetNewNumCommande();
                    SerialData _responseOpenInvoice = cmdOuvrirFacture(
                        _newNum,
                        "1",
                        invoiceData.OperatorName,
                        invoiceData.IFU,
                        "0.00",
                        _stringVAT,
                        invoiceData.FactureContent,
                        invoiceData.FactureRembContent,
                        "",
                        invoiceData.BuyerIFU,
                        invoiceData.BuyerName,
                        _stringAIB);

                    //if (string.IsNullOrEmpty(_responseOpenInvoice.Value) == false)
                    //{
                        //Ajout d'articles
                        //_newNum++;//= GetNewNumCommande();
                        SerialData _responseAddArticle = null;
                        foreach (InvoiceDataDetail _detail in invoiceData.Articles)
                        {
                            _stringVAT = _detail.VatApplied == false ? "A" : "B";
                            _stringPrice = _detail.PriceATI.ToString();
                            _stringQuantity = _detail.Quantity.ToString();
                            _stringSpecificTax = _detail.SpecificTAX.ToString();
                            _newNum++;
                            _responseAddArticle = cmdAddArticle(
                                _newNum,
                                _detail.ArticleName,
                                _detail.Description,
                               _stringVAT,
                               _stringPrice,
                                _stringQuantity,
                                _stringSpecificTax
                                );

                            _responseAddArticle = null;
                        }
                        _newNum++;///= GetNewNumCommande();
                        SerialData _responseValideInvoice = cmdVerifTotal(_newNum);
                        _newNum++;//= GetNewNumCommande();
                        SerialData _responseCloseInvoice = cmdFactStateSet(_newNum);
                        _responseMCF = _responseCloseInvoice.Value;
                    //}
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    throw ex;
                }
                catch (DecoderFallbackException ex)
                {
                    throw ex;
                }
                catch (ArgumentNullException ex)
                {
                    throw ex;
                }
                catch (ArgumentException ex)
                {
                    throw ex;
                }
                catch (InvalidOperationException ex)
                {
                    throw ex;
                }
                catch (System.ServiceProcess.TimeoutException ex)
                {
                    throw ex;
                }
                catch (ConfigurationErrorsException ex)
                {
                    throw ex;
                }

            }
            else
            {
                throw new ArgumentNullException(ExceptionString.ARGUMENT_INVOICE_DATA_EXCEPTION);
            }
            ClosePort();
            return _responseMCF;
        }

        #endregion

        #region writeToDevice

        /// <summary>
        /// Ecrit les donnèes sur le port du MCF
        /// </summary>
        /// <param name="order">commande à écrire</param>
        /// <param name="meaningfulldataindex">index du dernier octet de données</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="DecoderFallbackException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="System.ServiceProcess.TimeoutException"></exception>
        /// <returns></returns>
        private byte[] writeToDevice(byte[] order, out int meaningfulldataindex)
        {
            //Initialisation des variables
            byte[] answerFromComPort = new byte[256];
            int index = 0, endOfData = 0;

            if (_serialPort.IsOpen)
            {
                try
                {
                    _serialPort.Write(order, 0, order.Length);
                    //read from port
                    bool done = false;
                    while (!done)
                    {
                        byte current = (byte)_serialPort.ReadByte();
                        answerFromComPort[index++] = current;
                        switch (current)
                        {
                            case CommandString.MSG_NAK_MSG:
                                _serialPort.Write(order, 0, order.Length);
                                done = false;
                                index = 0;                      
                                break;
                            case CommandString.MSG_SYN_MSG:
                                index = 0;
                                //Thread.Sleep(100);
                                break;                                  
                            case CommandString.MSG_END_MDATA_BREAK:
                                endOfData = index;
                                break;
                            case CommandString.MSG_END_MESSAGE:
                                done = true;
                                break;                            
                        }                        
                          
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new ArgumentOutOfRangeException(ExceptionString.ARGUMENT_OUT_OF_RANGE_EXCEPTION);
                }
                catch (DecoderFallbackException)
                {
                    throw new DecoderFallbackException(ExceptionString.ARGUMENT_EXCEPTION);
                }
                catch (ArgumentNullException)
                {
                    throw new ArgumentNullException(ExceptionString.ARGUMENT_NULL_EXCEPTION);
                }
                catch (ArgumentException)
                {
                    throw new ArgumentException(ExceptionString.ARGUMENT_EXCEPTION);
                }
                catch (InvalidOperationException)
                {
                    throw new InvalidOperationException(ExceptionString.INVALID_OPERATION_EXCEPTION);
                }
                catch (System.ServiceProcess.TimeoutException)
                {
                    throw new System.ServiceProcess.TimeoutException(ExceptionString.TIME_OUT_EXCEPTION);
                }

            }
           
            meaningfulldataindex = endOfData;
            return answerFromComPort;
        }
        #endregion       

        #region buildMessage

        /// <summary>
        /// Permet de créer la commande à envoyer
        /// </summary>
        /// <param name="len">longeur occupé par les octets de données</param>
        /// <param name="seq">numéro de la commande</param>
        /// <param name="cmd">commande</param>
        /// <param name="data">données à envoyer</param>        
        /// <returns></returns>
        byte[] buildMessage(int len, int seq, byte cmd, string data)
        {
            //Initialisation des variables
            int minlenght = 10; //Longeur minimum du message
            byte[] _message = _encoderService.getByteFromString(data); //Données de la commande
            int _messagelenght = _message.Length;
            byte[] message = new byte[minlenght + _messagelenght]; //Message à envoyer
            int _currentDataIndex = 0; //Index actuel du message 
            byte[] _tabSumData = new byte[4]; //tableau contenant la somme des données pour vérification

            message[0] = CommandString.MSG_START_MESSAGE; //octet de début 
            //Somme des cellules obligatoire(4) plus longueur des données (_longData) et du décalage (32 en décimal)
            message[1] = Convert.ToByte(3 + _messagelenght + 1 + 32);
            message[2] = Convert.ToByte(seq + 32);//numéro de la commande
            message[3] = cmd; //code de la Commande à exécuter

            //Gestion de l'ajout de données de la commande
            if (_messagelenght == 0)
            {
                _messagelenght = 0;
                _currentDataIndex = 4;
            }
            else
            {
                for (int i = 4; i < (_messagelenght + 4); i++)
                {
                    message[i] = _message[i - 4];
                    _currentDataIndex = i + 1;
                }
            }

            message[_currentDataIndex] = CommandString.MSG_AMB;//Délimiteur fin des données

            //Ajout de octets de vérification
            _tabSumData = calculateCheckSum(message, 1, _message.Length + 4);
            message[_currentDataIndex + 1] = _tabSumData[0];
            message[_currentDataIndex + 2] = _tabSumData[1];
            message[_currentDataIndex + 3] = _tabSumData[2];
            message[_currentDataIndex + 4] = _tabSumData[3];
            message[_currentDataIndex + 5] = CommandString.MSG_END_MESSAGE; //Délimiteur fin de la commande

            return message;
        }
        #endregion

        #region cmdOuvrirFacture
        /// <summary>
        /// Permet d'ouvir une nouvelle facture
        /// </summary>
        /// <param name="commandeNum">Numéro de la commande</param>
        /// <param name="numoperateur">Numéro de l'opérateur</param>
        /// <param name="opnom">Nom de l'opérateur</param>
        /// <param name="ifu">identifiant fiscal unique</param>
        /// <param name="TAXA">TVA exempté</param>
        /// <param name="TAXB">TVA à 18%</param>
        /// <param name="typeFactureV">Type de facture de vente</param>
        /// <param name="typeFactureR">Type de facture de retour</param>
        /// <param name="serialnumber">Numero de la facture de vente en cas de retour</param>
        /// <param name="customerIFU">Numéro IFU du client</param>
        /// <param name="customerName">Nom du client</param>
        /// <param name="AIB">Type de AIB appliqué</param>
        /// <param name="commandData">commande d'ouverture de facture</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="DecoderFallbackException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="System.ServiceProcess.TimeoutException"></exception>
        /// <returns></returns>
        public SerialData cmdOuvrirFacture(int commandeNum,
                                            string numoperateur,
                                                     string opnom,
                                                     string ifu,
                                                     string TAXA,
                                                     string TAXB,
                                                     string typeFactureV,
                                                     string typeFactureR,
                                                     string serialnumber,
                                                     string customerIFU,
                                                     string customerName,
                                                     string AIB,
                                                     string TAXC = CommandString.FACT_TAXC,
                                                     string TAXD = CommandString.FACT_TAXD,
                                                      byte commandData = CommandString.FACT_DEBUT_FACTURE)
        {

            SerialData response = new SerialData(_decoderService);
            response.CONTENT_TYPE = SerialDataContentEnum.DEBUT_FACTURE;

            //Limitation du nom de l'opérateur
            if (opnom.Length > 32)
            {
                opnom = opnom.Remove(32);
            }

            //constitution des données de la commande
            //Données de base
            string _dataOfCmd = $"{numoperateur},{opnom},{ifu},{TAXA},{TAXB},{TAXC},{TAXD}";

            //Type de facture, cas des factures de remboursement, la référence est obligatoire
            if (string.IsNullOrEmpty(typeFactureV) == false && string.IsNullOrEmpty(typeFactureR) == true)
            {
                _dataOfCmd = $"{_dataOfCmd},{typeFactureV}";
            }
            else if (string.IsNullOrEmpty(typeFactureR) == false && string.IsNullOrEmpty(typeFactureV) == true)
            {
                _dataOfCmd = $"{_dataOfCmd},{typeFactureR},{serialnumber}";
            }
            else
            {
                throw new ArgumentNullException(paramName: "Référence facture", message: ExceptionString.REFERENCE_INVOICE_NULL);
            }

            //Ajout des informations clients si elles ne sont pas disponibles
            if (string.IsNullOrEmpty(customerIFU) == false && string.IsNullOrEmpty(customerName) == false && string.IsNullOrEmpty(AIB) == false)
            {
                _dataOfCmd = $"{_dataOfCmd},{customerIFU},{customerName},{AIB}";
            }            

            int _len = 4 + _dataOfCmd.Length;
            byte[] _message = buildMessage(_len, commandeNum, commandData, _dataOfCmd);
            int meaningfulldata = 0;
            try
            {
                response.EncodedData = writeToDevice(_message, out meaningfulldata);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw ex;
            }
            catch (DecoderFallbackException ex)
            {
                throw ex;
            }
            catch (ArgumentNullException ex)
            {
                throw ex;
            }
            catch (ArgumentException ex)
            {
                throw ex;
            }
            catch (InvalidOperationException ex)
            {
                throw ex;
            }
            catch (System.ServiceProcess.TimeoutException ex)
            {
                throw ex;
            }

            response.EndOfData = meaningfulldata;
            return response;
        }
        #endregion

        #region cmdLireEtat
        /// <summary>
        /// Permet de lire l'état du MCF
        /// </summary>
        /// <param name="commandData">commande de vérification d'état</param>
        /// <param name="commandeNum">Numéro de la commande</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="DecoderFallbackException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="System.ServiceProcess.TimeoutException"></exception>
        /// <returns></returns>
        public SerialData cmdLireEtat(int commandeNum, byte commandData = CommandString.ETAT_MCF)
        {
            SerialData response = new SerialData(_decoderService);
            response.CONTENT_TYPE = SerialDataContentEnum.ETAT_MCF;

            byte[] _message = buildMessage(4, commandeNum, commandData, "");
            int meaningfulldata = 0;
            try
            {
                response.EncodedData = writeToDevice(_message, out meaningfulldata);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw ex;
            }
            catch (DecoderFallbackException ex)
            {
                throw ex;
            }
            catch (ArgumentNullException ex)
            {
                throw ex;
            }
            catch (ArgumentException ex)
            {
                throw ex;
            }
            catch (InvalidOperationException ex)
            {
                throw ex;
            }
            catch (System.ServiceProcess.TimeoutException ex)
            {
                throw ex;
            }
            response.EndOfData = meaningfulldata;
            return response;
        }
        #endregion

        #region cmdAddArticle
        /// <summary>
        /// Ajoute un article à la facture
        /// </summary>
        /// <param name="commandeNum">Numéro de la commande</param>
        /// <param name="nomArticle">Nom de l'article</param>
        /// <param name="nomArticleComplement">Nom complémentaire de l'article</param>
        /// <param name="TaxAorB">Taxe applicable à l'article</param>
        /// <param name="PriceTTC">Prix TTC</param>
        /// <param name="Quantite">Quantité de l'article</param>
        /// <param name="TaxeSpec">Taxe spécifique appliquée</param>
        /// <param name="commandData">commande d'ajout d'article</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="DecoderFallbackException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="System.ServiceProcess.TimeoutException"></exception>
        /// <returns></returns>
        public SerialData cmdAddArticle(int commandeNum,
                                        string nomArticle,
                                        string nomArticleComplement,
                                        string TaxAorB,
                                        string PriceTTC,
                                        string Quantite,
                                        string TaxeSpec,
                                        byte commandData = CommandString.FACT_ENREGISTREMENT_ARTICLE
                                       )
        {
            SerialData response = new SerialData(_decoderService);//Absence de reponse
            int _len = 0;

            //Limitation de la longueur de certain paramètres
            if (nomArticle.Length > 30)
            {
                nomArticle = nomArticle.Remove(30);
            }
            if (string.IsNullOrEmpty(nomArticleComplement) == false)
            {
                if (nomArticleComplement.Length > 30)
                {
                    nomArticleComplement = nomArticleComplement.Remove(30);
                }
                nomArticleComplement = nomArticleComplement.PadLeft(1, '\n');//Ajout de a gauche 0Ah
            }

            //Constitution des données           
            string _dataCmd = $"{nomArticle}{nomArticleComplement}\t{TaxAorB}{PriceTTC}";

            //Ajout de la quantité si elle est différente de 1
            if (string.IsNullOrEmpty(Quantite) == false)
            {
                _dataCmd = $"{_dataCmd}*{Quantite}";
            }

            //Ajout de la taxe spécifique si elle est supérieur à 0
            if (string.IsNullOrEmpty(TaxeSpec) == false)
            {
                _dataCmd = $"{_dataCmd};{TaxeSpec}";
            }
            _len = 4 + _dataCmd.Length;
            byte[] _message = buildMessage(_len, commandeNum, commandData, _dataCmd);
            int meaningfulldata = 0;
            try
            {
                response.EncodedData = writeToDevice(_message, out meaningfulldata);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw ex;
            }
            catch (DecoderFallbackException ex)
            {
                throw ex;
            }
            catch (ArgumentNullException ex)
            {
                throw ex;
            }
            catch (ArgumentException ex)
            {
                throw ex;
            }
            catch (InvalidOperationException ex)
            {
                throw ex;
            }
            catch (System.ServiceProcess.TimeoutException ex)
            {
                throw ex;
            }
            response.EndOfData = meaningfulldata;

            return response;
        }
        #endregion

        #region cmdLireSubTotal       
        /// <summary>
        /// Permet d'obtenir le sous total de la facture en cours
        /// </summary>
        /// <param name="commandeNum">Numéro de la commande</param>
        /// <param name="commandData">commande de récupération du sous total</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="DecoderFallbackException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="System.ServiceProcess.TimeoutException"></exception>
        /// <returns></returns>
        public SerialData cmdLireSubTotal(int commandeNum, byte commandData = CommandString.FACT_SOUS_TOTAL)
        {
            SerialData response = new SerialData(_decoderService);
            response.CONTENT_TYPE = SerialDataContentEnum.SOUS_TOTAL;

            byte[] _message = buildMessage(4, commandeNum, commandData, "");
            int meaningfulldata = 0;
            try
            {
                response.EncodedData = writeToDevice(_message, out meaningfulldata);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw ex;
            }
            catch (DecoderFallbackException ex)
            {
                throw ex;
            }
            catch (ArgumentNullException ex)
            {
                throw ex;
            }
            catch (ArgumentException ex)
            {
                throw ex;
            }
            catch (InvalidOperationException ex)
            {
                throw ex;
            }
            catch (System.ServiceProcess.TimeoutException ex)
            {
                throw ex;
            }
            response.EndOfData = meaningfulldata;
            return response;
        }
        #endregion

        #region cmdVerifTotal
        /// <summary>
        /// Permet de valider la facture avant de récupérer les informations de certification
        /// </summary>
        /// <param name="commandeNum">Numéro de la commande</param>
        /// <param name="commandData">commande de validation</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="DecoderFallbackException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="System.ServiceProcess.TimeoutException"></exception>
        /// <returns></returns>
        public SerialData cmdVerifTotal(int commandeNum, byte commandData = CommandString.FACT_TOTAL)
        {
            SerialData response = new SerialData(_decoderService);
            response.CONTENT_TYPE = SerialDataContentEnum.TOTAL;

            byte[] _message = buildMessage(4, 1, commandData, "");
            int meaningfulldata = 0;
            try
            {
                response.EncodedData = writeToDevice(_message, out meaningfulldata);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw ex;
            }
            catch (DecoderFallbackException ex)
            {
                throw ex;
            }
            catch (ArgumentNullException ex)
            {
                throw ex;
            }
            catch (ArgumentException ex)
            {
                throw ex;
            }
            catch (InvalidOperationException ex)
            {
                throw ex;
            }
            catch (System.ServiceProcess.TimeoutException ex)
            {
                throw ex;
            }
            response.EndOfData = meaningfulldata;
            return response;
        }
        #endregion

        #region cmdFactStateSet       

        /// <summary>
        /// Permet de finaliser une facture en cours
        /// </summary>    
        /// <param name="commandeNum">Numéro de la commande</param>
        /// <param name="commandData">commande de finalisation de facture</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="DecoderFallbackException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="System.ServiceProcess.TimeoutException"></exception>
        /// <returns></returns>
        public SerialData cmdFactStateSet(int commandeNum, byte commandData = CommandString.FACT_FIN_FACTURE)
        {
            SerialData response = new SerialData(_decoderService);
            response.CONTENT_TYPE = SerialDataContentEnum.FIN_FACTURE;

            byte[] _message = buildMessage(4, commandeNum, commandData, "");
            int meaningfulldata = 0;
            try
            {
                response.EncodedData = writeToDevice(_message, out meaningfulldata);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw ex;
            }
            catch (DecoderFallbackException ex)
            {
                throw ex;
            }
            catch (ArgumentNullException ex)
            {
                throw ex;
            }
            catch (ArgumentException ex)
            {
                throw ex;
            }
            catch (InvalidOperationException ex)
            {
                throw ex;
            }
            catch (System.ServiceProcess.TimeoutException ex)
            {
                throw ex;
            }
            response.EndOfData = meaningfulldata;
            return response;
        }
        #endregion

        #region cmdGetInfoContrib
        /// <summary>
        /// Récupère les informations du contribuable
        /// </summary>
        /// <param name="commandeNum">Numéro de la commande</param>
        /// <param name="infoKEY">clé de l'information</param>
        /// <param name="commandData">commande de récupération de d'information contribuable</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="DecoderFallbackException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="System.ServiceProcess.TimeoutException"></exception>
        /// <returns></returns>
        public SerialData cmdGetInfoContrib(int commandeNum, string infoKEY, byte commandData = CommandString.ETAT_INFO_CONTRIBUABLE)
        {
            SerialData response = new SerialData(_decoderService);
            response.CONTENT_TYPE = SerialDataContentEnum.INFO_CONTRIBUABLE;

            byte[] _message = buildMessage(6, commandeNum, commandData, infoKEY);
            int meaningfulldata = 0;
            try
            {
                response.EncodedData = writeToDevice(_message, out meaningfulldata);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw ex;
            }
            catch (DecoderFallbackException ex)
            {
                throw ex;
            }
            catch (ArgumentNullException ex)
            {
                throw ex;
            }
            catch (ArgumentException ex)
            {
                throw ex;
            }
            catch (InvalidOperationException ex)
            {
                throw ex;
            }
            catch (System.ServiceProcess.TimeoutException ex)
            {
                throw ex;
            }
            response.EndOfData = meaningfulldata;
            return response;
        }
        #endregion

        #region cmdCheckServerConnection

        /// <summary>
        /// Permet de vérifier l'état de connexion avec le serveur
        /// </summary>
        /// <param name="commandeNum">Numéro de la commande</param>
        /// <param name="commandData">commande de vérification de connexion</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="DecoderFallbackException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="System.ServiceProcess.TimeoutException"></exception>
        /// <returns></returns>
        /// <returns></returns>
        public SerialData cmdCheckServerConnection(int commandeNum, byte commandData = CommandString.ETAT_SERVEUR)
        {
            SerialData response = new SerialData(_decoderService);
            response.CONTENT_TYPE = SerialDataContentEnum.ETAT_CONNEXION;

            byte[] _message = buildMessage(4, commandeNum, commandData, "");
            int meaningfulldata = 0;
            try
            {
                response.EncodedData = writeToDevice(_message, out meaningfulldata);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw ex;
            }
            catch (DecoderFallbackException ex)
            {
                throw ex;
            }
            catch (ArgumentNullException ex)
            {
                throw ex;
            }
            catch (ArgumentException ex)
            {
                throw ex;
            }
            catch (InvalidOperationException ex)
            {
                throw ex;
            }
            catch (System.ServiceProcess.TimeoutException ex)
            {
                throw ex;
            }
            response.EndOfData = meaningfulldata;
            return response;
        }

        #endregion

        #region calculateCheckSum
        /// <summary>
        /// Permet de créer les éléments des bites de vérification
        /// </summary>
        /// <param name="command">tableau de la commande</param>
        /// <param name="start">index de début</param>
        /// <param name="end">index de fin</param>
        /// <returns></returns>
        private byte[] calculateCheckSum(byte[] command, int start, int end)
        {
            byte[] _resultatTab = new byte[4];
            var _somme = 0;
            for (int i = start; i <= end; i++)
            {
                _somme += command[i];
            }
            _resultatTab[0] = (byte)(((_somme >> 12) & 0x0F) + 0x30);
            _resultatTab[1] = (byte)(((_somme >> 8) & 0x0F) + 0x30);
            _resultatTab[2] = (byte)(((_somme >> 4) & 0x0F) + 0x30);
            _resultatTab[3] = (byte)(((_somme >> 0) & 0x0F) + 0x30);
            return _resultatTab;
        }
        #endregion       

        #region ClosePort

        /// <summary>
        /// Permet de fermer le port de communication
        /// </summary>
        /// <exception cref="System.IO.IOException"></exception>
        public void ClosePort()
        {
            try
            {
                //Ferme le port de communication
                _serialPort.Close();
            }
            catch (System.IO.IOException ex)
            {
                throw ex;
            }
        }
        #endregion
    }
}
