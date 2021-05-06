using System.ServiceModel;
using System.Threading.Tasks;
using AfipServiceReference;
using AfipWebServicesClient.Extensions;
using AfipWebServicesClient.Model;
using ServiceSoapClient = AfipServiceReference.ServiceSoapClient;

namespace AfipWebServicesClient
{
    public class WsfeClient
    {
        private readonly ServiceSoapClient _wsfeService;
        private bool IsProdEnvironment { get; }
        private long Cuit { get; }
        private string Token { get; }
        private string Sign { get; }

        private static string TestingEnvironment => "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";
        private static string ProductionEnvironment => "https://servicios1.afip.gov.ar/wsfev1/service.asmx";

        public WsfeClient(long cuit, string token, string sign, bool isProdEnvironment)
        {
            Cuit = cuit;
            Token = token;
            IsProdEnvironment = isProdEnvironment;
            Sign = sign;
            _wsfeService = new ServiceSoapClient(ServiceSoapClient.EndpointConfiguration.ServiceSoap);
            _wsfeService.Endpoint.Address = new EndpointAddress(IsProdEnvironment ? ProductionEnvironment : TestingEnvironment);
        }

        public async Task<FECompUltimoAutorizadoResponse> GetUltimoAutorizadoAsync(int puntoDeVenta, TipoComprobante tipoComprobante)
        {
            var auth = new FEAuthRequest { Cuit = Cuit, Sign = Sign, Token = Token };
            var response = await _wsfeService.FECompUltimoAutorizadoAsync(auth, puntoDeVenta, tipoComprobante.ToInt());
            return response;
        }

        public async Task<FEParamGetPtosVentaResponse> GetSalesPointAsync()
        {
            var auth = new FEAuthRequest { Cuit = Cuit, Sign = Sign, Token = Token };
            var response = await _wsfeService.FEParamGetPtosVentaAsync(auth);
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
