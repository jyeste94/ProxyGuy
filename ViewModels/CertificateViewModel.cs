using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using ProxyGuy.Helpers;

namespace ProxyGuy.ViewModels;

public partial class CertificateViewModel : ObservableObject
{
    [ObservableProperty]
    private bool certificateExists;

    [ObservableProperty]
    private bool certificateInstalled;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string installInstructions = string.Empty;

    public CertificateViewModel()
    {
        RefreshStatus();
        InstallInstructions = CertificateHelper.GetInstallationInstructions();
    }

    [RelayCommand]
    private void RefreshStatus()
    {
        CertificateExists = CertificateHelper.CertificateExists();
        CertificateInstalled = CertificateHelper.IsCertificateInstalled();

        if (!CertificateExists)
        {
            StatusMessage = "Certificado no encontrado. Inicia el proxy para generarlo.";
            return;
        }

        StatusMessage = CertificateInstalled
            ? "Certificado instalado correctamente."
            : "Certificado generado pero no instalado.";
    }

    [RelayCommand]
    private async Task ExportCertificate()
    {
        try
        {
            var pfxPath = CertificateHelper.GetCertificatePath();
            if (!File.Exists(pfxPath))
            {
                await ShowAlert("Error", "No se encontro el certificado raiz.");
                return;
            }

            var tempCer = Path.Combine(FileSystem.CacheDirectory, "ProxyGuy_Root.cer");
            if (CertificateHelper.ExportCertificateForInstallation(tempCer) == null)
            {
                await ShowAlert("Error", "Fallo la exportacion del certificado.");
                return;
            }

            await using var stream = File.OpenRead(tempCer);
            var result = await FileSaver.Default.SaveAsync("ProxyGuy_Root.cer", stream, CancellationToken.None);

            if (result.IsSuccessful)
            {
                await ShowAlert("Exito", $"Certificado guardado en: {result.FilePath}");
            }
        }
        catch (Exception ex)
        {
            CrashLogger.Log("CertificateViewModel.ExportCertificate", ex);
            await ShowAlert("Error", $"Error al exportar: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task InstallCertificate()
    {
        try
        {
            var success = CertificateHelper.InstallCertificate();
            if (!success)
            {
                await ShowAlert("Error", "No se pudo instalar. Ejecuta como Administrador o instalalo manualmente.");
                return;
            }

            await ShowAlert("Exito", "Certificado instalado en el almacen de confianza.");
            RefreshStatus();
        }
        catch (Exception ex)
        {
            CrashLogger.Log("CertificateViewModel.InstallCertificate", ex);
            await ShowAlert("Error", $"Excepcion: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task UninstallCertificate()
    {
        try
        {
            var success = CertificateHelper.UninstallCertificate();
            if (!success)
            {
                await ShowAlert("Error", "No se pudo desinstalar.");
                return;
            }

            await ShowAlert("Exito", "Certificado eliminado.");
            RefreshStatus();
        }
        catch (Exception ex)
        {
            CrashLogger.Log("CertificateViewModel.UninstallCertificate", ex);
            await ShowAlert("Error", $"Excepcion: {ex.Message}");
        }
    }

    private static Task ShowAlert(string title, string message)
    {
        var page = Application.Current?.MainPage;
        if (page == null)
            return Task.CompletedTask;

        return page.DisplayAlert(title, message, "OK");
    }
}
