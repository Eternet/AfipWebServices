using System;

namespace AfipWebServicesClient;

public class WsaaTicket
{
    // ReSharper disable CommentTypo
    public string Sign { get; set; } = "";       // Firma de seguridad recibida en la respuesta
    public string Token { get; set; } = "";      // Token de seguridad recibido en la respuesta
    public DateTime ExpirationTime { get; set; } //Expiracion del ticket
}
