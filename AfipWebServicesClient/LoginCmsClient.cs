using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AfipWebServicesClient
{
    public class AfipEnvironment
    {
        public bool IsProduction { get; }
        public string CertificateFile { get; }
        public string Password { get; }

        public AfipEnvironment(bool isProduction, string certFile, string password)
        {
            IsProduction = isProduction;
            CertificateFile = certFile;
            Password = password;
        }
    }
    // ReSharper disable StringLiteralTypo
    // ReSharper disable CommentTypo
    public class LoginCmsClient
    {
        public bool IsProduction { get; }
        public string TestingEnvironment { get; set; } = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";
        public string ProductionEnvironment { get; set; } = "https://wsaa.afip.gov.ar/ws/services/LoginCms";

        private static string TicketCacheFolderPath => "";

        public string CertificateFile;
        public string Password;
        public string XmlStrLoginTicketRequestTemplate = "<loginTicketRequest><header><uniqueId></uniqueId><generationTime></generationTime><expirationTime></expirationTime></header><service></service></loginTicketRequest>";
        private bool _verboseMode = true;
        private static uint _globalUniqueId; // NO ES THREAD-SAFE

        public LoginCmsClient(AfipEnvironment environment)
        {
            IsProduction = environment.IsProduction;
            CertificateFile = environment.CertificateFile;
            Password = environment.Password;
        }

        public async Task<WsaaTicket> LoginCmsAsync(string service, bool verbose = false)
        {
            var ticketCacheFile = string.IsNullOrEmpty(TicketCacheFolderPath) ?
                                        service + "ticket.json" :
                                        TicketCacheFolderPath + service + "ticket.json";

            if (File.Exists(ticketCacheFile))
            {
                var ticketJson = await File.ReadAllTextAsync(ticketCacheFile);
                var ticket = JsonConvert.DeserializeObject<WsaaTicket>(ticketJson);
                if (ticket is { } t && DateTime.UtcNow <= t.ExpirationTime)
                    return ticket;
            }

            const string idFnc = "[ObtenerLoginTicketResponse]";
            _verboseMode = verbose;
            X509CertificateManager.VerboseMode = verbose;

            // PASO 1: Genero el Login Ticket Request
            var ticketRequest = GenerateTicketRequest(service, idFnc);

            // PASO 2: Firmo el Login Ticket Request
            var base64SignedCms = SignLoginTicketRequest(ticketRequest, idFnc);

            // PASO 3: Invoco al WSAA para obtener el Login Ticket Response
            var loginTicketResponse = await GetLoginTicketResponse(idFnc, base64SignedCms);

            // PASO 4: Analizo el Login Ticket Response recibido del WSAA
            var ticketResponse = CreateTicketResponse(ticketCacheFile, idFnc, loginTicketResponse);

            return ticketResponse;
        }

        /*
            public uint UniqueId;           // Entero de 32 bits sin signo que identifica el requerimiento
            public DateTime GenerationTime; // Momento en que fue generado el requerimiento
            public DateTime ExpirationTime; // Momento en el que expira la solicitud
            public string? Sign;             // Firma de seguridad recibida en la respuesta
            public string? Token;            // Token de seguridad recibido en la respuesta
        */
        private static WsaaTicket CreateTicketResponse(
            string ticketCacheFile,
            string idFnc,
            string loginTicketResponse)
        {
            var xmlLoginTicketResponse = new XmlDocument();
            xmlLoginTicketResponse.LoadXml(loginTicketResponse);

            if (xmlLoginTicketResponse.SelectSingleNode("//uniqueId")?.InnerText is not { } strUniqueId)
                throw new Exception($"{idFnc}   Error LoginTicketResponse : uniqueId is null");

            if (!uint.TryParse(strUniqueId, out _))
                throw new Exception($"{idFnc}   Error LoginTicketResponse : can't parse uniqueId to uint");

            if (!DateTime.TryParse(xmlLoginTicketResponse.SelectSingleNode("//generationTime")?.InnerText, out _))
                throw new Exception($"{idFnc}   Error LoginTicketResponse : can't parse GenerationTime");

            if (!DateTime.TryParse(xmlLoginTicketResponse.SelectSingleNode("//expirationTime")?.InnerText, out var expirationTime))
                throw new Exception($"{idFnc}   Error LoginTicketResponse : can't parse GenerationTime");

            if (xmlLoginTicketResponse.SelectSingleNode("//sign")?.InnerText is not { } sign)
                throw new Exception($"{idFnc}   Error LoginTicketResponse : sign is null");

            if (xmlLoginTicketResponse.SelectSingleNode("//token")?.InnerText is not { } token)
                throw new Exception($"{idFnc}   Error LoginTicketResponse : token is null");

            var ticketResponse = new WsaaTicket { Sign = sign, Token = token, ExpirationTime = expirationTime };
            File.WriteAllText(ticketCacheFile, JsonConvert.SerializeObject(ticketResponse));
            return ticketResponse;

        }

        private async Task<string> GetLoginTicketResponse(string idFnc, string base64SignedCms)
        {
            string loginTicketResponse;
            try
            {
                if (_verboseMode)
                {
                    Console.WriteLine(idFnc + "***Llamando al WSAA en URL: {0}", IsProduction ? ProductionEnvironment : TestingEnvironment);
                    Console.WriteLine(idFnc + "***Argumento en el request:");
                    Console.WriteLine(base64SignedCms);
                }

                var wsaaService = new AfipLoginCmsServiceReference.LoginCMSClient();
                wsaaService.Endpoint.Address = new EndpointAddress(IsProduction ? ProductionEnvironment : TestingEnvironment);

                var response = await wsaaService.loginCmsAsync(base64SignedCms);
                loginTicketResponse = response.loginCmsReturn;

                if (_verboseMode)
                {
                    Console.WriteLine(idFnc + "***LoguinTicketResponse: ");
                    Console.WriteLine(loginTicketResponse);
                }

            }
            catch (Exception ex)
            {
                throw new Exception(idFnc + "***Error INVOCANDO al servicio WSAA : " + ex.Message);
            }

            return loginTicketResponse;
        }

        private string SignLoginTicketRequest(XmlNode? ticketRequest, string idFnc)
        {
            if (ticketRequest is null)
            {
                Console.WriteLine(idFnc + "XmlLoginTicketRequest is null");
                return string.Empty;
            }
            string base64SignedCms;
            if (string.IsNullOrEmpty(CertificateFile))
            {
                Console.WriteLine(idFnc + "CertificatePath is null");
                return string.Empty;
            }
            try
            {
                if (_verboseMode) 
                    Console.WriteLine(idFnc + "***Leyendo certificado: {0}", CertificateFile);

                var securePassword = new NetworkCredential("", Password).SecurePassword;
                securePassword.MakeReadOnly();

                var certSignatory = X509CertificateManager.GetCertificateFromFile(CertificateFile, securePassword);

                // Convierto el Login Ticket Request a bytes, firmo el msg y lo convierto a Base64
                var msgEncoding = Encoding.UTF8;
                var msgBytes = msgEncoding.GetBytes(ticketRequest.OuterXml);
                var encodedSignedCms = X509CertificateManager.SignMessageBytes(msgBytes, certSignatory);
                base64SignedCms = Convert.ToBase64String(encodedSignedCms);
            }
            catch (Exception ex)
            {
                throw new Exception(idFnc + "***Error FIRMANDO el LoginTicketRequest : " + ex.Message);
            }

            return base64SignedCms;
        }

        private XmlDocument GenerateTicketRequest(string service, string idFnc)
        {
            try
            {
                _globalUniqueId += 1;

                var xmlLoginTicketRequest = new XmlDocument();
                xmlLoginTicketRequest.LoadXml(XmlStrLoginTicketRequestTemplate);

                var xmlNodeUniqueId = xmlLoginTicketRequest.SelectSingleNode("//uniqueId");
                var xmlNodeGenerationTime = xmlLoginTicketRequest.SelectSingleNode("//generationTime");
                var xmlNodeExpirationTime = xmlLoginTicketRequest.SelectSingleNode("//expirationTime");
                var xmlNodeService = xmlLoginTicketRequest.SelectSingleNode("//service");

                if (xmlNodeGenerationTime is not null)
                    xmlNodeGenerationTime.InnerText = DateTime.Now.AddMinutes(-10).ToString("s");

                if (xmlNodeExpirationTime is not null)
                    xmlNodeExpirationTime.InnerText = DateTime.Now.AddMinutes(+10).ToString("s");

                if (xmlNodeUniqueId is not null)
                    xmlNodeUniqueId.InnerText = Convert.ToString(_globalUniqueId);
                
                if (xmlNodeService is not null)
                    xmlNodeService.InnerText = service;

                return xmlLoginTicketRequest;
            }
            catch (Exception ex)
            {
                throw new Exception(idFnc + "***Error GENERANDO el LoginTicketRequest : " + ex.Message + ex.StackTrace);
            }
        }
    }
}
