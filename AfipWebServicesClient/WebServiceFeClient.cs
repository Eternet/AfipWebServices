using AfipServiceReference;
using AfipWebServicesClient.Extensions;
using AfipWebServicesClient.Model;
using System.ServiceModel;
using System.Threading.Tasks;

namespace AfipWebServicesClient
{
    public class WebServiceFeClient
    {
        private readonly ServiceSoap _wsfeService;
        private bool IsProdEnvironment { get; }
        private long Cuit { get; }
        private string Token { get; }
        private string Sign { get; }

        private static string TestingEnvironment => "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";
        private static string ProductionEnvironment => "https://servicios1.afip.gov.ar/wsfev1/service.asmx";

        public WebServiceFeClient(long cuit, string token, string sign, bool isProdEnvironment, IAfipFeSoapClientFactory afipSoapClientFactory)
        {
            Cuit = cuit;
            Token = token;
            IsProdEnvironment = isProdEnvironment;
            Sign = sign;
            _wsfeService = afipSoapClientFactory.CreateClient(new EndpointAddress(IsProdEnvironment ? ProductionEnvironment : TestingEnvironment));
        }

        public async Task<FECompUltimoAutorizadoResponse> GetLastAuthorizedAsync(int salePoint, TipoComprobante voucherType)
        {
            var auth = new FEAuthRequest { Cuit = Cuit, Sign = Sign, Token = Token };
            var request = new FECompUltimoAutorizadoRequest
            {
                Body = new FECompUltimoAutorizadoRequestBody(auth, salePoint, voucherType.ToInt())
            };
            var response = await _wsfeService.FECompUltimoAutorizadoAsync(request);
            return response;
        }

        public async Task<FEParamGetPtosVentaResponse> GetSalesPointAsync()
        {
            var auth = new FEAuthRequest { Cuit = Cuit, Sign = Sign, Token = Token };
            var request = new FEParamGetPtosVentaRequest { Body = new FEParamGetPtosVentaRequestBody(auth) };
            var response = await _wsfeService.FEParamGetPtosVentaAsync(request);
            return response;
        }

        // ReSharper disable InconsistentNaming
        public async Task<FECAESolicitarResponse> GetCaeAsync(FECAERequest feCaeReq)
        {
            var auth = new FEAuthRequest { Cuit = Cuit, Sign = Sign, Token = Token };
            var request = new FECAESolicitarRequest { Body = new FECAESolicitarRequestBody(auth, feCaeReq) };
            var response = await _wsfeService.FECAESolicitarAsync(request);
            return response;
        }

    }
}
