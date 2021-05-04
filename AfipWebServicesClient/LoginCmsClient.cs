using System;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;

namespace AfipWebServicesClient
{
    // ReSharper disable StringLiteralTypo
    // ReSharper disable CommentTypo
    public class LoginCmsClient
    {
        public bool IsProdEnvironment { get; set; } = false;
        public string TestingEnvironment { get; set; } = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";
        public string ProductionEnvironment { get; set; } = "https://wsaa.afip.gov.ar/ws/services/LoginCms";

        public string TicketCacheFolderPath { get; set; } = "";

        public uint UniqueId;           // Entero de 32 bits sin signo que identifica el requerimiento
        public DateTime GenerationTime; // Momento en que fue generado el requerimiento
        public DateTime ExpirationTime; // Momento en el que expira la solicitud
        public string? Service;          // Identificacion del WSN para el cual se solicita el TA
        public string? Sign;             // Firma de seguridad recibida en la respuesta
        public string? Token;            // Token de seguridad recibido en la respuesta
        public XmlDocument? XmlLoginTicketRequest;
        //public XmlDocument? XmlLoginTicketResponse;
        public string? CertificatePath;
        public string XmlStrLoginTicketRequestTemplate = "<loginTicketRequest><header><uniqueId></uniqueId><generationTime></generationTime><expirationTime></expirationTime></header><service></service></loginTicketRequest>";
        private bool _verboseMode = true;
        private static uint _globalUniqueId; // NO ES THREAD-SAFE

        public async Task<WsaaTicket> LoginCmsAsync(string service,
                                                    string x509CertificateFilePath,
                                                    string password,
                                                    bool verbose)
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
            CertificatePath = x509CertificateFilePath;
            _verboseMode = verbose;
            X509CertificateManager.VerboseMode = verbose;

            // PASO 1: Genero el Login Ticket Request
            GenerateTicketRequest(service, idFnc);

            // PASO 2: Firmo el Login Ticket Request
            var base64SignedCms = SignLoginTicketRequest(password, idFnc);

            // PASO 3: Invoco al WSAA para obtener el Login Ticket Response
            var loginTicketResponse = await GetLoginTicketResponse(idFnc, base64SignedCms);

            // PASO 4: Analizo el Login Ticket Response recibido del WSAA
            var ticketResponse = CreateTicketResponse(ticketCacheFile, idFnc, loginTicketResponse);

            return ticketResponse;
        }

        private WsaaTicket CreateTicketResponse(
            string ticketCacheFile, 
            string idFnc, 
            string loginTicketResponse)
        {
            try
            {
                var xmlLoginTicketResponse = new XmlDocument();
                xmlLoginTicketResponse.LoadXml(loginTicketResponse);

                if (xmlLoginTicketResponse.SelectSingleNode("//uniqueId")?.InnerText is not { } strUniqueId)
                    throw new Exception($"{idFnc}   Error LoginTicketResponse : uniqueId is null");

                if (!uint.TryParse(strUniqueId, out var uniqueId))
                    throw new Exception($"{idFnc}   Error LoginTicketResponse : can't parset uniqueId to uint");

                GenerationTime = DateTime.Parse(xmlLoginTicketResponse.SelectSingleNode("//generationTime").InnerText);
                ExpirationTime = DateTime.Parse(xmlLoginTicketResponse.SelectSingleNode("//expirationTime").InnerText);
                Sign = xmlLoginTicketResponse.SelectSingleNode("//sign").InnerText;
                Token = xmlLoginTicketResponse.SelectSingleNode("//token").InnerText;
            }
            catch (Exception ex)
            {
                throw new Exception(idFnc + "   Error LoginTicketResponse : " + ex.Message);
            }

            var ticketResponse = new WsaaTicket { Sign = Sign, Token = Token, ExpirationTime = ExpirationTime };
            File.WriteAllText(ticketCacheFile, JsonConvert.SerializeObject(ticketResponse));
            return ticketResponse;
        }

        private async Task<string> GetLoginTicketResponse(string ID_FNC, string base64SignedCms)
        {
            string loginTicketResponse;
            try
            {
                if (_verboseMode)
                {
                    Console.WriteLine(ID_FNC + "***Llamando al WSAA en URL: {0}", IsProdEnvironment ? ProductionEnvironment : TestingEnvironment);
                    Console.WriteLine(ID_FNC + "***Argumento en el request:");
                    Console.WriteLine(base64SignedCms);
                }

                var wsaaService = new AfipLoginCmsServiceReference.LoginCMSClient();
                wsaaService.Endpoint.Address = new EndpointAddress(IsProdEnvironment ? ProductionEnvironment : TestingEnvironment);

                var response = await wsaaService.loginCmsAsync(base64SignedCms);
                loginTicketResponse = response.loginCmsReturn;

                if (_verboseMode)
                {
                    Console.WriteLine(ID_FNC + "***LoguinTicketResponse: ");
                    Console.WriteLine(loginTicketResponse);
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ID_FNC + "***Error INVOCANDO al servicio WSAA : " + ex.Message);
            }

            return loginTicketResponse;
        }

        private string SignLoginTicketRequest(string password, string ID_FNC)
        {
            string base64SignedCms;
            try
            {
                if (_verboseMode) Console.WriteLine(ID_FNC + "***Leyendo certificado: {0}", CertificatePath);

                var securePassword = new NetworkCredential("", password).SecurePassword;
                securePassword.MakeReadOnly();


                var certFirmante = X509CertificateManager.GetCertificateFromFile(CertificatePath, securePassword);

                if (_verboseMode)
                {
                    Console.WriteLine(ID_FNC + "***Firmando: ");
                    Console.WriteLine(XmlLoginTicketRequest.OuterXml);
                }

                // Convierto el Login Ticket Request a bytes, firmo el msg y lo convierto a Base64
                var msgEncoding = Encoding.UTF8;
                var msgBytes = msgEncoding.GetBytes(XmlLoginTicketRequest.OuterXml);
                var encodedSignedCms = X509CertificateManager.SignMessageBytes(msgBytes, certFirmante);
                base64SignedCms = Convert.ToBase64String(encodedSignedCms);
            }
            catch (Exception ex)
            {
                throw new Exception(ID_FNC + "***Error FIRMANDO el LoginTicketRequest : " + ex.Message);
            }

            return base64SignedCms;
        }

        private void GenerateTicketRequest(string service, string ID_FNC)
        {
            try
            {
                _globalUniqueId += 1;

                XmlLoginTicketRequest = new XmlDocument();
                XmlLoginTicketRequest.LoadXml(XmlStrLoginTicketRequestTemplate);

                var xmlNodoUniqueId = XmlLoginTicketRequest.SelectSingleNode("//uniqueId");
                var xmlNodoGenerationTime = XmlLoginTicketRequest.SelectSingleNode("//generationTime");
                var xmlNodoExpirationTime = XmlLoginTicketRequest.SelectSingleNode("//expirationTime");
                var xmlNodoService = XmlLoginTicketRequest.SelectSingleNode("//service");
                xmlNodoGenerationTime.InnerText = DateTime.Now.AddMinutes(-10).ToString("s");
                xmlNodoExpirationTime.InnerText = DateTime.Now.AddMinutes(+10).ToString("s");
                xmlNodoUniqueId.InnerText = Convert.ToString(_globalUniqueId);
                xmlNodoService.InnerText = service;
                Service = service;

                if (_verboseMode) Console.WriteLine(XmlLoginTicketRequest.OuterXml);
            }
            catch (Exception ex)
            {
                throw new Exception(ID_FNC + "***Error GENERANDO el LoginTicketRequest : " + ex.Message + ex.StackTrace);
            }
        }
    }
}
