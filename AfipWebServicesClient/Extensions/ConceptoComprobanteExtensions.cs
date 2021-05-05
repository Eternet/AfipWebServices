using AfipWebServicesClient.Model;

namespace AfipWebServicesClient.Extensions
{
    public static class EnumsExtensions
    {
        public static int ToInt(this ConceptoComprobante conceptoComprobante)
        {
            return (int)conceptoComprobante;
        }
        public static int ToInt(this TipoComprobante tipoComprobante)
        {
            return (int)tipoComprobante;
        }
    }
}