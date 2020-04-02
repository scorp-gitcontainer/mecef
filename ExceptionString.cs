using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sc.apps.lib.mcf.Const
{
    public class ExceptionString
    {
        public const string REFERENCE_INVOICE_NULL = "Une erreur s'est produite, la référence de la facture de remboursement est obligatoire";
        public const string OVERFLOW_EXCEPTION = "Une erreur s'est produite, la valeur d'une opération dépasse la limite du type, cause probable:";
        public const string ARGUMENT_NULL_EXCEPTION = "Une erreur s'est produite, un ou plusieurs paramètres sont null, cause probable:";
        public const string INVALID_OPERATION_EXCEPTION = "Une erreur s'est produite, l'opération demandée n'est pas disponible, cause probable:";
        public const string ARGUMENT_OUT_OF_RANGE_EXCEPTION = "Une erreur s'est produite, la valeur d'un paramètre dépasse la plage du type, cause probable:";
        public const string ARGUMENT_EXCEPTION = "Une erreur s'est produite, la valeur d'un paramètre n'est pas valide, cause probable:";
        public const string ARGUMENT_INVOICE_DATA_EXCEPTION = "Une erreur s'est produite, la classe de donnée fournie est nulle, cause probable:";
        public const string TIME_OUT_EXCEPTION = "Une erreur s'est produite, le temps de réponse du MCF est dépassé, cause probable:";
        public const string DECODER_FALL_BACK_EXCEPTION = "Une erreur s'est produite, la conversion de la reponse a échoué ,cause probable:";
        public const string CONFIGURATION_ERROR_EXCEPTION = "Une erreur s'est produite, le paramètre commandeNum n'est pas accessible " +
            "dans le fichier de configuration ,cause probable:";
    }
}
