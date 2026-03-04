using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Maui.ApplicationModel;

namespace ProxyGuy.Helpers;

public static class CertificateHelper
{
    private const string CertificateFileName = "rootCert.pfx";
    private const string CertificatePassword = ""; // Titanium.Web.Proxy uses empty password by default

    /// <summary>
    /// Gets the full path to the root certificate file.
    /// </summary>
    public static string GetCertificatePath()
    {
        return Path.Combine(FileSystem.AppDataDirectory, CertificateFileName);
    }

    /// <summary>
    /// Checks if the root certificate file exists.
    /// </summary>
    public static bool CertificateExists()
    {
        return File.Exists(GetCertificatePath());
    }

    /// <summary>
    /// Checks if the ProxyGuy certificate is installed in the Windows trust store.
    /// </summary>
    public static bool IsCertificateInstalled()
    {
        try
        {
            using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(
                X509FindType.FindByIssuerName,
                "ProxyGuy Root CA",
                validOnly: false
            );

            return certificates.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Exports the root certificate to a .cer file for manual installation.
    /// Returns the path to the exported certificate, or null if failed.
    /// </summary>
    public static string? ExportCertificateForInstallation(string destinationPath)
    {
        try
        {
            var pfxPath = GetCertificatePath();
            if (!File.Exists(pfxPath))
                return null;

            // Load the PFX certificate
            var cert = new X509Certificate2(pfxPath, CertificatePassword);

            // Export as .cer (DER encoded)
            var cerBytes = cert.Export(X509ContentType.Cert);
            File.WriteAllBytes(destinationPath, cerBytes);

            return destinationPath;
        }
        catch (Exception ex)
        {
            CrashLogger.Log("CertificateHelper.ExportCertificate", ex);
            return null;
        }
    }

    /// <summary>
    /// Gets installation instructions for the user.
    /// </summary>
    public static string GetInstallationInstructions()
    {
        return @"Para instalar el certificado manualmente:

1. Haz doble clic en el archivo .cer exportado
2. Haz clic en 'Instalar certificado...'
3. Selecciona 'Usuario actual' y haz clic en 'Siguiente'
4. Selecciona 'Colocar todos los certificados en el siguiente almac?n'
5. Haz clic en 'Examinar' y selecciona 'Entidades de certificaci?n ra?z de confianza'
6. Haz clic en 'Siguiente' y luego en 'Finalizar'
7. Confirma la advertencia de seguridad

Despu?s de instalar, reinicia el navegador para que los cambios surtan efecto.";
    }

    /// <summary>
    /// Attempts to install the certificate programmatically (requires admin rights).
    /// Returns true if successful.
    /// </summary>
    public static bool InstallCertificate()
    {
        try
        {
            var pfxPath = GetCertificatePath();
            if (!File.Exists(pfxPath))
                return false;

            var cert = new X509Certificate2(pfxPath, CertificatePassword);

            using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);
            store.Close();

            return true;
        }
        catch (Exception ex)
        {
            CrashLogger.Log("CertificateHelper.InstallCertificate", ex);
            return false;
        }
    }

    /// <summary>
    /// Removes the ProxyGuy certificate from the Windows trust store.
    /// </summary>
    public static bool UninstallCertificate()
    {
        try
        {
            using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            var certificates = store.Certificates.Find(
                X509FindType.FindByIssuerName,
                "ProxyGuy Root CA",
                validOnly: false
            );

            foreach (var cert in certificates)
            {
                store.Remove(cert);
            }

            store.Close();
            return true;
        }
        catch (Exception ex)
        {
            CrashLogger.Log("CertificateHelper.UninstallCertificate", ex);
            return false;
        }
    }
}
