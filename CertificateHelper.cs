using System;
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

    }
}
