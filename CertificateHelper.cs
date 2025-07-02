using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Titanium.Web.Proxy;

namespace ProxyGuy.WinForms
{
    public static class CertificateHelper
    {
        public static void InstallRootCertificate(ProxyServer proxyServer)
        {
            // 1) Crear si no existe
            if (proxyServer.CertificateManager.RootCertificate == null)
            {
                bool ok = proxyServer.CertificateManager.CreateRootCertificate(persistToFile: true);
                if (!ok) throw new InvalidOperationException("No se pudo crear el certificado raíz");
            }

            // 2) Confiar en el almacén de usuario si aún no está confiado
            if (!proxyServer.CertificateManager.IsRootCertificateUserTrusted())
            {
                proxyServer.CertificateManager.TrustRootCertificate(machineTrusted: false);
            }
        }

        public static Task InstallRootCertificateAsync(ProxyServer proxyServer)
        {
            InstallRootCertificate(proxyServer);
            return Task.CompletedTask;
        }

        public static string ExportRootCertificatePem(ProxyServer proxyServer)
        {
            var cert = proxyServer.CertificateManager.RootCertificate;
            if (cert == null) throw new InvalidOperationException("Certificado no disponible");

            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProxyGuy");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "proxy-root-cert.pem");

            var bytes = cert.Export(X509ContentType.Cert);
            var base64 = Convert.ToBase64String(bytes, Base64FormattingOptions.InsertLineBreaks);
            var pem = $"-----BEGIN CERTIFICATE-----{Environment.NewLine}{base64}{Environment.NewLine}-----END CERTIFICATE-----";
            File.WriteAllText(path, pem);
            return path;
        }

    }
}
