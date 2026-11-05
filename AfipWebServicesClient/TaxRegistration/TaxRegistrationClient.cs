using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AfipWebServicesClient.TaxRegistration;

public interface ITaxRegistrationClient
{
    Task<TaxRegistrationResult> Get(string cuit, AfipEnvironment environment, CancellationToken cancellationToken);
}

/// <summary>SOAP adapter using the current XML contract without depending on the legacy generated A5 proxy.</summary>
public sealed class TaxRegistrationClient(HttpClient httpClient) : ITaxRegistrationClient
{
    private static readonly SemaphoreSlim LoginGate = new(1, 1);

    public async Task<TaxRegistrationResult> Get(string cuit, AfipEnvironment environment, CancellationToken cancellationToken)
    {
        cuit = TaxId.Normalize(cuit);
        WsaaTicket ticket;
        await LoginGate.WaitAsync(cancellationToken);
        try
        {
            // Reuse WSAA's service-specific ticket cache. Never use a wsfe ticket for the padrón.
            ticket = await new LoginCmsClient(environment).LoginCmsAsync("ws_sr_constancia_inscripcion");
        }
        catch (Exception)
        {
            throw new TaxRegistrationException("authentication_failed", "No se pudo autenticar la consulta del padrón en ARCA.");
        }
        finally { LoginGate.Release(); }

        XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
        XNamespace a5 = "http://a5.soap.ws.server.puc.sr/";
        var body = new XDocument(new XElement(soap + "Envelope", new XElement(soap + "Body",
            new XElement(a5 + "getPersona_v2", new XElement("token", ticket.Token), new XElement("sign", ticket.Sign),
                new XElement("cuitRepresentada", environment.Cuit), new XElement("idPersona", cuit)))));
        var endpoint = environment.IsProduction
            ? "https://aws.arca.gob.ar/sr-padron/webservices/personaServiceA5"
            : "https://awshomo.arca.gob.ar/sr-padron/webservices/personaServiceA5";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.TryAddWithoutValidation("SOAPAction", "\"\"");
        request.Content = new StringContent(body.ToString(), Encoding.UTF8, "text/xml");
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new TaxRegistrationException("padron_unavailable", "ARCA no pudo completar la consulta. Volvé a intentar.");
            var xml = await response.Content.ReadAsStringAsync(cancellationToken);
            return TaxRegistrationParser.Parse(xml, cuit, DateTime.UtcNow);
        }
        catch (HttpRequestException)
        {
            throw new TaxRegistrationException("padron_unavailable", "No se pudo conectar con el padrón de ARCA.");
        }
    }
}
