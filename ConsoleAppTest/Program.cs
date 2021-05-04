using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AfipWebServicesClient;
using AfipServiceReference;

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
            var loginClient = new LoginCmsClient { IsProdEnvironment = true };

            WsaaTicket wsfeTicket;
            WsaaTicket wscdcTicket;
            try
            {
                //wsfeTicket = await loginClient.LoginCmsAsync("wsfe",
                //    @"C:\Fuentes\Afip\Certs\cert.pfx",
                //    "diegotes",
                //    true);

                //wscdcTicket = await loginClient.LoginCmsAsync("wscdc",
                //    @"C:\Fuentes\Afip\Certs\cert.pfx",
                //    "diegotes",
                //    true);

                wsfeTicket = await loginClient.LoginCmsAsync("wsfe",
                    @"C:\Fuentes\Afip\Certs\Eternet\Eternet.pfx",
                    "diegotes",
                    true);

                wscdcTicket = await loginClient.LoginCmsAsync("wscdc",
                    @"C:\Fuentes\Afip\Certs\Eternet\Eternet.pfx",
                    "diegotes",
                    true);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            var wsfeClient = new WsfeClient(loginClient.IsProdEnvironment)
            {
                //Cuit = 20250229209,
                Cuit = 30667525906,
                Sign = wsfeTicket.Sign,
                Token = wsfeTicket.Token
            };

            var wscdcClient = new WscdcClient(loginClient.IsProdEnvironment)
            {
                //Cuit = 20250229209,
                Cuit = 30667525906,
                Sign = wscdcTicket.Sign,
                Token = wscdcTicket.Token
            };

            var comprobantesTipoConsultarResponse = await wscdcClient.GetVoucherTypesAsync();
            var json = JsonConvert.SerializeObject(comprobantesTipoConsultarResponse, Formatting.Indented);
            Console.WriteLine(json);
            await File.WriteAllTextAsync("ComprobantesTipoConsultarResponse.json", json);

            //Get next WSFE Comp. Number
            var last = await wsfeClient.GetLastAuthorizedAsync(31, 6);
            json = JsonConvert.SerializeObject(last, Formatting.Indented);
            Console.WriteLine(json);
            Console.ReadLine();
            //var compNumber = last.Body.FECompUltimoAutorizadoResult.CbteNro + 1;

            //var now = DateTime.Now;
            //var monthInit = new DateTime(now.Year,now.Month,1);
            //var nextMonth = now.AddDays(30);
            ////Build WSFE FECAERequest            
            //var feCaeReq = new FECAERequest
            //{
            //    FeCabReq = new FECAECabRequest
            //    {
            //        // ReSharper disable CommentTypo
            //        CantReg = 1, //Cantidad de registros del detalle del comprobante o lote de comprobantes de ingreso
            //        CbteTipo = 6, //Tipo de comprobante que se está informando. Si se informa más de un comprobante, todos deben ser del mismo tipo
            //        PtoVta = 1 // Punto de Venta del comprobante que se está informando. Si se informa más de un comprobante, todos deben corresponder al mismo punto de venta.
            //    },
            //    FeDetReq = new List<FECAEDetRequest>
            //    {
            //        new FECAEDetRequest
            //        {
            //            CbteDesde = compNumber,
            //            CbteHasta = compNumber+100,
            //            CbteFch = AfipFormatDate(now),
            //            Concepto = ConceptoComprobante.Servicios.ToInt(),
            //            DocNro = 0, //Para individual DNI del cliente: 30111222
            //            DocTipo = 99, //Código de documento identificatorio del comprador //Para individual 96
            //            FchVtoPago = AfipFormatDate(nextMonth),
            //            ImpNeto = 10,
            //            ImpTotal = 10,
            //            FchServDesde = AfipFormatDate(monthInit),
            //            FchServHasta = AfipFormatDate(now),
            //            MonCotiz = 1,
            //            MonId = "PES",
            //            Iva = new List<AlicIva>
            //            {
            //                new AlicIva
            //                {
            //                    BaseImp = 10,
            //                    Id = 3,
            //                    Importe = 0
            //                }
            //            }
            //        },
            //    }
            //};

            ////Call WSFE FECAESolicitar
            //var compResult = await wsfeClient.GetCAEAsync(feCaeReq);
            //json = JsonConvert.SerializeObject(compResult, Formatting.Indented);
            //Console.ReadLine();
            //Console.Clear();
            //Console.WriteLine(json);
            //await File.WriteAllTextAsync("FECAESolicitarResponse.json", json);
        }

        private static string AfipFormatDate(DateTime dateTime)
        {
            return $"{dateTime.Year}{dateTime.Month:00}{dateTime.Day:00}";
        }
    }
}
