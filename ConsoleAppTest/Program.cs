using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AfipWebServicesClient;
using Newtonsoft.Json;

namespace ConsoleAppTest
{
    public enum ConceptoComprobante
    {
        Productos = 1,
        Servicios = 2,
        ProductosYServicios = 3
    }

    public static class ConceptoComprobanteExtensions
    {
        public static int ToInt(this ConceptoComprobante conceptoComprobante)
        {
            return (int) conceptoComprobante;
        }
    }


    internal class Program
    {

        
        private static async Task Main()
        {
            //Get Login Ticket
            var loginClient = new LoginCmsClient { IsProdEnvironment = false };

            WsaaTicket wsfeTicket;
            WsaaTicket wscdcTicket;
            try
            {
                wsfeTicket = await loginClient.LoginCmsAsync("wsfe",
                    @"C:\Fuentes\Afip\Certs\cert.pfx",
                    "diegotes",
                    true);

                wscdcTicket = await loginClient.LoginCmsAsync("wscdc",
                    @"C:\Fuentes\Afip\Certs\cert.pfx",
                    "diegotes",
                    true);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            var wsfeClient = new WsfeClient
            {
                IsProdEnvironment = false,
                Cuit = 20250229209,
                Sign = wsfeTicket.Sign,
                Token = wsfeTicket.Token
            };

            var wscdcClient = new WscdcClient
            {
                IsProdEnvironment = false,
                Cuit = 20250229209,
                Sign = wscdcTicket.Sign,
                Token = wscdcTicket.Token
            };
            
            var comprobantesTipoConsultarResponse = await wscdcClient.GetVoucherTypesAsync();
            var json = JsonConvert.SerializeObject(comprobantesTipoConsultarResponse, Formatting.Indented);
            await File.WriteAllTextAsync("ComprobantesTipoConsultarResponse.json", json);

            //Get next WSFE Comp. Number
            var last = await wsfeClient.GetLastAuthorizedAsync(1, 6);
            var compNumber = last.Body.FECompUltimoAutorizadoResult.CbteNro + 1;

            //Build WSFE FECAERequest            
            var feCaeReq = new AfipServiceReference.FECAERequest
            {
                FeCabReq = new AfipServiceReference.FECAECabRequest
                {
                    // ReSharper disable CommentTypo
                    CantReg = 1, //Cantidad de registros del detalle del comprobante o lote de comprobantes de ingreso
                    CbteTipo = 6, //Tipo de comprobante que se está informando. Si se informa más de un comprobante, todos deben ser del mismo tipo
                    PtoVta = 1 // Punto de Venta del comprobante que se está informando. Si se informa más de un comprobante, todos deben corresponder al mismo punto de venta.
                },
                FeDetReq = new List<AfipServiceReference.FECAEDetRequest>
                {
                    new AfipServiceReference.FECAEDetRequest
                    {
                        CbteDesde = compNumber,
                        CbteHasta = compNumber,
                        CbteFch = "20190717",
                        Concepto = ConceptoComprobante.Servicios.ToInt(),
                        DocNro = 30111222,
                        DocTipo = 96, //Código de documento identificatorio del comprador
                        FchVtoPago = "20190718",
                        ImpNeto = 10,
                        ImpTotal = 10,
                        FchServDesde = "20190717",
                        FchServHasta = "20190717",
                        MonCotiz = 1,
                        MonId = "PES",
                        Iva = new List<AfipServiceReference.AlicIva>
                        {
                            new AfipServiceReference.AlicIva
                            {
                                BaseImp = 10,
                                Id = 3,
                                Importe = 0
                            }
                        }
                    }
                }
            };

            //Call WSFE FECAESolicitar
            var compResult = await wsfeClient.FECAESolicitarAsync(feCaeReq);
            json = JsonConvert.SerializeObject(compResult, Formatting.Indented);
            await File.WriteAllTextAsync("FECAESolicitarResponse.json", json);
        }
    }
}
