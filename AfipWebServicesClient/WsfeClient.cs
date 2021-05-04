using System.ServiceModel;
using System.Threading.Tasks;
using AfipServiceReference;
using ServiceSoapClient = AfipServiceReference.ServiceSoapClient;

namespace AfipWebServicesClient
{
    public class WsfeClient
    {
        public bool IsProdEnvironment { get; set; } = false;
        public long Cuit { get; set; }
        public string Token { get; set; } = "";
        public string Sign { get; set; } = "";

        public string TestingEnvironment { get; set; } = "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";
        public string ProductionEnvironment { get; set; } = "https://servicios1.afip.gov.ar/wsfev1/service.asmx";

        public async Task<FECompUltimoAutorizadoResponse> GetLastAuthorizedAsync(int salesPoint, int voucherType)
        {
            var wsfeService = new ServiceSoapClient(ServiceSoapClient.EndpointConfiguration.ServiceSoap);
            wsfeService.Endpoint.Address = new EndpointAddress(IsProdEnvironment ? ProductionEnvironment : TestingEnvironment);

            var auth = new FEAuthRequest { Cuit = Cuit, Sign = Sign, Token = Token };
            var response = await wsfeService.FECompUltimoAutorizadoAsync(auth, salesPoint, voucherType);
            return response;
        }

        // ReSharper disable InconsistentNaming
        public async Task<FECAESolicitarResponse> FECAESolicitarAsync(FECAERequest feCaeReq)
        {
            var wsfeService = new ServiceSoapClient(ServiceSoapClient.EndpointConfiguration.ServiceSoap);
            wsfeService.Endpoint.Address = new EndpointAddress(IsProdEnvironment ? ProductionEnvironment : TestingEnvironment);

            var auth = new FEAuthRequest { Cuit = Cuit, Sign = Sign, Token = Token };

            var response = await wsfeService.FECAESolicitarAsync(auth, feCaeReq);

            return response;
        }

    }
}
