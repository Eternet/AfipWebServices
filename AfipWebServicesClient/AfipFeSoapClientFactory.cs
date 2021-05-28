using Microsoft.Extensions.Logging;
using System.ServiceModel;
using System.Threading.Tasks;
using ServiceSoapClient = AfipServiceReference.ServiceSoapClient;

namespace AfipWebServicesClient
{
    public interface IAfipFeSoapClientFactory
    {
        public AfipServiceReference.ServiceSoap CreateClient(EndpointAddress endpointAddress);
        public Task<WebServiceFeClient> CreateClientFromEnvironment(bool isProduction);
    }

    public class AfipFeSoapClientFactory : IAfipFeSoapClientFactory
    {
        private readonly ILogger<LoginCmsClient> _logger;
        private readonly AfipEnvironments _afipEnvironments;

        public AfipFeSoapClientFactory(ILogger<LoginCmsClient> logger, AfipEnvironments afipEnvironments)
        {
            _logger = logger;
            _afipEnvironments = afipEnvironments;
        }

        public AfipServiceReference.ServiceSoap CreateClient(EndpointAddress endpointAddress)
        {
            var client = new ServiceSoapClient(ServiceSoapClient.EndpointConfiguration.ServiceSoap);
            client.Endpoint.Address = endpointAddress;
            return client;
        }

        public async Task<WebServiceFeClient> CreateClientFromEnvironment(bool isProduction)
        {
            var environment = _afipEnvironments.GetAfipEnvironment(isProduction: isProduction);
            var loginClient = new LoginCmsClient(environment, _logger);
            var wsfeTicket = await loginClient.LoginCmsAsync("wsfe");
            var wsfeClient = new WebServiceFeClient(environment.Cuit, wsfeTicket.Token, wsfeTicket.Sign, isProduction, this);
            return wsfeClient;
        }
    }
}
