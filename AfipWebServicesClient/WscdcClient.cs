using System.ServiceModel;
using System.Threading.Tasks;
using AfipWscdcServiceReference;

namespace AfipWebServicesClient;

public class WscdcClient
{
    private readonly ServiceSoapClient _wscdcService;
    private bool IsProdEnvironment { get; }
    public long Cuit { get; set; }
    public string Token { get; set; } = "";
    public string Sign { get; set; } = "";

    public string TestingEnvironment { get; set; } = "https://wswhomo.afip.gov.ar/WSCDC/service.asmx";
    public string ProductionEnvironment { get; set; } = "https://servicios1.afip.gov.ar/WSCDC/service.asmx";

    public WscdcClient(bool isProdEnvironment)
    {
        IsProdEnvironment = isProdEnvironment;
        _wscdcService = new ServiceSoapClient(ServiceSoapClient.EndpointConfiguration.ServiceSoap);
        _wscdcService.Endpoint.Address = new EndpointAddress(IsProdEnvironment ? ProductionEnvironment : TestingEnvironment);
    }

    public async Task<ComprobanteConstatarResponse> VerifyVoucherAsync(CmpDatos cmpReq)
    {

        var auth = new CmpAuthRequest { Cuit = Cuit, Sign = Sign, Token = Token };
        var response = await _wscdcService.ComprobanteConstatarAsync(auth, cmpReq);
        return response;
    }

    public async Task<ComprobantesTipoConsultarResponse> GetVoucherTypesAsync()
    {
        var auth = new CmpAuthRequest { Cuit = Cuit, Sign = Sign, Token = Token };
        var response = await _wscdcService.ComprobantesTipoConsultarAsync(auth);
        return response;
    }

}
