using System;
using System.Security;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace AfipWebServicesClient
{    
    // ReSharper disable StringLiteralTypo
    public class X509CertificateManager
    {

        public static byte[] SignMessageBytes(byte[] messageBytes, X509Certificate2 signerCertificate)
        {
            try
            {
                var contentInfo = new ContentInfo(messageBytes);
                var signedCms = new SignedCms(contentInfo);

                var cmsSigner = new CmsSigner(signerCertificate)
                {
                    IncludeOption = X509IncludeOption.EndCertOnly
                };

                signedCms.ComputeSignature(cmsSigner);
                return signedCms.Encode();
            }
            catch (Exception ex)
            {
                throw new Exception("X509CertificateManager.SignMessageBytes: " + ex.Message);
            }
        }

        public static X509Certificate2 GetCertificateFromFile(string file, SecureString password)
        {
            try
            {
                var objCert = new X509Certificate2(file, password);
                return objCert;
            }
            catch (Exception ex)
            {
                throw new Exception("X509CertificateManager.GetCertificateFromFile: " + ex.Message);
            }
        }
    }
}
