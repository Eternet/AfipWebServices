using System.ServiceModel;
using System.Threading.Tasks;
using AfipWscdcServiceReference;

namespace AfipWebServicesClient
{
    public class WscdcClient
    {
        public bool IsProdEnvironment { get; set; } = false;
        public long Cuit { get; set; }
        public string Token { get; set; } = "";
        public string Sign { get; set; } = "";

        public string TestingEnvironment { get; set; } = "https://wswhomo.afip.gov.ar/WSCDC/service.asmx";
        public string ProductionEnvironment { get; set; } = "https://servicios1.afip.gov.ar/WSCDC/service.asmx";

        public async Task<ComprobanteConstatarResponse> VerifyVoucherAsync(CmpDatos cmpReq)
        {
            var wscdcService = new ServiceSoapClient(ServiceSoapClient.EndpointConfiguration.ServiceSoap);
            wscdcService.Endpoint.Address = new EndpointAddress(IsProdEnvironment ? ProductionEnvironment : TestingEnvironment);

            var auth = new CmpAuthRequest { Cuit = Cuit, Sign = Sign, Token = Token };
            var response = await wscdcService.ComprobanteConstatarAsync(auth, cmpReq);

            return response;
        }

        public async Task<ComprobantesTipoConsultarResponse> GetVoucherTypesAsync()
        {
            var wscdcService = new ServiceSoapClient(ServiceSoapClient.EndpointConfiguration.ServiceSoap);
            wscdcService.Endpoint.Address = new EndpointAddress(IsProdEnvironment ? ProductionEnvironment : TestingEnvironment);

            var auth = new CmpAuthRequest { Cuit = Cuit, Sign = Sign, Token = Token };
            var response = await wscdcService.ComprobantesTipoConsultarAsync(auth);
            return response;
        }

    }
}
