using AfipWebServicesClient;
using AfipWebServicesClient.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace ConsoleAppTest
{
    internal class Program
    {
        private static async Task Main()
        {
            var current = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var prodCert = Path.Combine(current, @"Certs\Eternet\Eternet.pfx");
            var testCert = Path.Combine(current, @"Certs\Testing\cert.pfx");

            var envProd = new AfipEnvironment(30667525906, true, prodCert, "diegotes");
            var envTest = new AfipEnvironment(20250229209, false, testCert, "diegotes");
            var envs = new AfipEnvironments(envProd, envTest);
            //Get Login Ticket
            var logger = new Mock<ILogger<LoginCmsClient>>().Object;
            var clientFabric = new AfipFeSoapClientFactory(logger, envs);
            var wsfeClient = await clientFabric.CreateClientFromEnvironment(isProduction: false);

            //var wscdcClient = new WscdcClient(loginClient.IsProdEnvironment)
            //{
            //    //Cuit = 20250229209,
            //    Cuit = 30667525906,
            //    Sign = wscdcTicket.Sign,
            //    Token = wscdcTicket.Token
            //};

            //var comprobantesTipoConsultarResponse = await wscdcClient.GetVoucherTypesAsync();
            //var json = JsonConvert.SerializeObject(comprobantesTipoConsultarResponse, Formatting.Indented);
            //Console.WriteLine(json);
            //await File.WriteAllTextAsync("ComprobantesTipoConsultarResponse.json", json);

            //Get next WSFE Comp. Number
            var taxesTypes = await wsfeClient.GetTaxesTypesAsync();
            foreach(var i in taxesTypes.Body.FEParamGetTiposTributosResult.ResultGet)
            {
                Console.WriteLine($"{i.Desc.Replace(" ","")}={i.Id}");
            }
            var json = JsonConvert.SerializeObject(taxesTypes, Formatting.Indented);            
            await File.WriteAllTextAsync("ComprobantesTipoConsultarResponse.json", json);

            Console.ReadLine();
            //var compNumber = last.Body.FECompUltimoAutorizadoResult.CbteNro + 1;

            //var now = DateTime.Now;
            //var monthInit = new DateTime(now.Year, now.Month, 1);
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
            //            CbteHasta = compNumber,
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
            //var jsonResult = JsonConvert.SerializeObject(compResult, Formatting.Indented);
            //Console.ReadLine();
            //Console.Clear();
            //Console.WriteLine(jsonResult);
            //await File.WriteAllTextAsync("FECAESolicitarResponse.json", jsonResult);
        }

        private static string AfipFormatDate(DateTime dateTime)
        {
            return $"{dateTime.Year}{dateTime.Month:00}{dateTime.Day:00}";
        }
    }
}
