using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sc.apps.lib.mcf.Const
{
    public class CommandString
    {
        public const byte ETAT_MCF = 0xC1;
        public const byte ETAT_SERVEUR = 0xC2;
        public const byte ETAT_INFO_CONTRIBUABLE = 0x2B;

        public const byte FACT_DEBUT_FACTURE = 0xC0;
        public const byte FACT_ENREGISTREMENT_ARTICLE = 0x31;
        public const byte FACT_SOUS_TOTAL = 0x33;
        public const byte FACT_TOTAL = 0x35;
        public const byte FACT_FIN_FACTURE = 0x38;

        public const byte MSG_MDATA_START = 0x01;
        public const byte MSG_END_MDATA_BREAK = 0x04;
        public const byte MSG_END_MESSAGE = 0x03;
        public const byte MSG_START_MESSAGE = 0x01;
        public const byte MSG_AMB = 0x05;

        public const string FACT_TYPE_VENTENORMALE = "FV";
        public const string FACT_TYPE_COPIEVENTE = "CV";
        public const string FACT_TYPE_REMBOURSEMENTNORMAL = "FR";
        public const string FACT_AIB_VIDE = "";
        public const string FACT_AIB_1 = "AIB1";
        public const string FACT_AIB_2 = "AIB2";

        public const string FACT_TAXC = "0.00";
        public const string FACT_TAXD = "18.00";

    }
    
}
