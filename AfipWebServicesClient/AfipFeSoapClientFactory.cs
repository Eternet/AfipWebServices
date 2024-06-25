using Microsoft.Extensions.Logging;
using System;
using System.ServiceModel;
using System.Threading.Tasks;
using ServiceSoapClient = AfipServiceReference.ServiceSoapClient;

namespace AfipWebServicesClient;

public interface IAfipFeSoapClientFactory
{
    public AfipServiceReference.ServiceSoap CreateClient(EndpointAddress endpointAddress);
    public ValueTask<WebServiceFeClient> CreateClientFromEnvironment(bool isProduction);
}


/// <summary>
/// AfipFeSoapClientFactory must to be singleton!!!
/// </summary>
public class AfipFeSoapClientFactory(
    ILogger<LoginCmsClient> logger,
    AfipEnvironments afipEnvironments) : IAfipFeSoapClientFactory
{
    private WsaaTicket? _ticketProduction;
    private WsaaTicket? _ticketTesting;
    private WebServiceFeClient? _clientProduction;
    private WebServiceFeClient? _clientTesting;

    public AfipServiceReference.ServiceSoap CreateClient(EndpointAddress endpointAddress)
    {
        var client = new ServiceSoapClient(ServiceSoapClient.EndpointConfiguration.ServiceSoap);
        client.Endpoint.Address = endpointAddress;
        return client;
    }

    public async ValueTask<WebServiceFeClient> CreateClientFromEnvironment(bool isProduction)
    {
        if (isProduction)
        {
            if (_ticketProduction is { } ticketProduction &&
                DateTime.Now < ticketProduction.ExpirationTime &&
                _clientProduction is { } clientProduction)
                return clientProduction;
            return await CreateNewClient(isProduction);
        }
        else
        {
            if (_ticketTesting is { } ticketTesting &&
                DateTime.Now < ticketTesting.ExpirationTime &&
                _clientTesting is { } clientTesting)
                return clientTesting;
            return await CreateNewClient(isProduction);
        }
    }

    private async ValueTask<WebServiceFeClient> CreateNewClient(bool isProduction)
    {
        try
        {
            var environment = afipEnvironments.GetAfipEnvironment(isProduction: isProduction);
            var loginClient = new LoginCmsClient(environment, logger);
            var wsfeTicket = await loginClient.LoginCmsAsync("wsfe");
            var wsfeClient = new WebServiceFeClient(environment.Cuit, wsfeTicket.Token, wsfeTicket.Sign, isProduction, this);
            if (isProduction)
            {
                _clientProduction = wsfeClient;
                _ticketProduction = wsfeTicket;
            }
            else
            {
                _clientTesting = wsfeClient;
                _ticketTesting = wsfeTicket;
            }
            return wsfeClient;
        }
        catch (Exception ex)
        {
            _clientProduction = null;
            _ticketProduction = null;
            _clientTesting = null;
            _ticketTesting = null;
            logger.LogError(ex, $"AfipFeSoapClientFactory.CreateNewClient - Error: {ex.Message}");
            throw;
        }            
    }
}
