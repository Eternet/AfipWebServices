using System;
using System.Security;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace AfipWebServicesClient
{
    // ReSharper disable StringLiteralTypo
    public class X509CertificateManager
    {
        public static bool VerboseMode = false;

        public static byte[] SignMessageBytes(byte[] messageBytes, X509Certificate2 signerCertificate)
        {
            const string idFnc = "[FirmaBytesMensaje]";
            try
            {
                var contentInfo = new ContentInfo(messageBytes);
                var signedCms = new SignedCms(contentInfo);

                var cmsSigner = new CmsSigner(signerCertificate)
                {
                    IncludeOption = X509IncludeOption.EndCertOnly
                };

                if (VerboseMode) 
                    Console.WriteLine(idFnc + "***Firmando bytes del mensaje...");

                signedCms.ComputeSignature(cmsSigner);

                if (VerboseMode) 
                    Console.WriteLine(idFnc + "***OK mensaje firmado");

                return signedCms.Encode();
            }
            catch (Exception ex)
            {
                throw new Exception(idFnc + "***Error al firmar: " + ex.Message);
            }
        }

        public static X509Certificate2 GetCertificateFromFile(string file, SecureString password)
        {
            const string idFnc = "[ObtieneCertificadoDesdeArchivo]";
            try
            {
                var objCert = new X509Certificate2(file, password);
                return objCert;
            }
            catch (Exception ex)
            {
                throw new Exception(idFnc + "***Error al leer certificado: " + ex.Message);
            }
        }
    }
}
