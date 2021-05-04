using System.ServiceModel;
using System.Threading.Tasks;
using AfipServiceReference;
using ServiceSoapClient = AfipServiceReference.ServiceSoapClient;

namespace AfipWebServicesClient
{
    public class WsfeClient
    {
        private readonly ServiceSoapClient _wsfeService;
        private bool IsProdEnvironment { get; }
        public long Cuit { get; set; }
        public string Token { get; set; } = "";
        public string Sign { get; set; } = "";

        public string TestingEnvironment { get; set; } = "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";
        public string ProductionEnvironment { get; set; } = "https://servicios1.afip.gov.ar/wsfev1/service.asmx";

        public WsfeClient(bool isProdEnvironment)
        {
            IsProdEnvironment = isProdEnvironment;
            _wsfeService = new ServiceSoapClient(ServiceSoapClient.EndpointConfiguration.ServiceSoap);
            _wsfeService.Endpoint.Address = new EndpointAddress(IsProdEnvironment ? ProductionEnvironment : TestingEnvironment);
        }

        public async Task<FECompUltimoAutorizadoResponse> GetLastAuthorizedAsync(int salesPoint, int voucherType)
        {
            var auth = new FEAuthRequest { Cuit = Cuit, Sign = Sign, Token = Token };
            var response = await _wsfeService.FECompUltimoAutorizadoAsync(auth, salesPoint, voucherType);
            return response;
        }

        // ReSharper disable InconsistentNaming
        public async Task<FECAESolicitarResponse> GetCAEAsync(FECAERequest feCaeReq)
        {
            var auth = new FEAuthRequest { Cuit = Cuit, Sign = Sign, Token = Token };
            var response = await _wsfeService.FECAESolicitarAsync(auth, feCaeReq);
            return response;
        }

    }
}
