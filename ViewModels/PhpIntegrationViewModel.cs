using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using ProxyGuy.Services;

namespace ProxyGuy.ViewModels;

public partial class PhpIntegrationViewModel : ObservableObject
{
    public PhpIntegrationManager Manager => PhpIntegrationManager.Instance;

    [RelayCommand]
    private async Task AddPath()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select php.ini file",
                FileTypes = null // All files, or specific if platform allows custom type for .ini
            });

            if (result != null)
            {
                if (!result.FileName.Equals("php.ini", StringComparison.OrdinalIgnoreCase))
                {
                    await ShowAlert("Archivo no valido", "Selecciona un archivo php.ini.");
                    return;
                }

                if (!Manager.PhpIniPaths.Contains(result.FullPath))
                {
                    Manager.PhpIniPaths.Add(result.FullPath);
                    // If proxy is already running, we might want to patch it immediately? 
                    // Or wait for restart. For safety, let's just add it.
                    // User usually sets this up before debugging.
                }
            }
        }
        catch (Exception ex)
        {
            CrashLogger.Log("PhpIntegrationViewModel.AddPath", ex);
            await ShowAlert("Error", $"No se pudo agregar la ruta: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RemovePath(string path)
    {
        if (Manager.PhpIniPaths.Contains(path))
        {
            Manager.PhpIniPaths.Remove(path);
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
