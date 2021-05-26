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
        private readonly AfipEnvironments _afipEnvironments;

        public AfipFeSoapClientFactory(AfipEnvironments afipEnvironments)
        {
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
            var loginClient = new LoginCmsClient(environment);
            var wsfeTicket = await loginClient.LoginCmsAsync("wsfe", true);
            var wsfeClient = new WebServiceFeClient(environment.Cuit, wsfeTicket.Token, wsfeTicket.Sign, isProduction, this);
            return wsfeClient;
        }
    }
}
