using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using ProxyGuy.Models;
using ProxyGuy.Services;

namespace ProxyGuy.ViewModels;

public partial class MapLocalViewModel : ObservableObject
{
    public ObservableCollection<MapLocalRule> Rules => RuleManager.Instance.Rules;

    [ObservableProperty]
    private string newPattern = "api/v1/user";

    [ObservableProperty]
    private string newFilePath = string.Empty;

    [ObservableProperty]
    private int newStatusCode = 200;

    [RelayCommand]
    private async Task PickFile()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync();
            if (result != null)
            {
                NewFilePath = result.FullPath;
            }
        }
        catch
        {
            // Ignore cancel
        }
    }

    [RelayCommand]
    private async Task AddRule()
    {
        if (string.IsNullOrWhiteSpace(NewPattern) || string.IsNullOrWhiteSpace(NewFilePath))
        {
            await ShowAlert("Datos incompletos", "Debes indicar patron y archivo local.");
            return;
        }

        if (!File.Exists(NewFilePath))
        {
            await ShowAlert("Archivo no valido", "El archivo local seleccionado no existe.");
            return;
        }

        if (NewStatusCode < 100 || NewStatusCode > 599)
        {
            await ShowAlert("Status invalido", "El status debe estar entre 100 y 599.");
            return;
        }

        if (Rules.Any(r => string.Equals(r.UrlPattern, NewPattern, StringComparison.OrdinalIgnoreCase)
                           && string.Equals(r.LocalFilePath, NewFilePath, StringComparison.OrdinalIgnoreCase)))
        {
            await ShowAlert("Regla duplicada", "Ya existe una regla con ese patron y archivo.");
            return;
        }

        var rule = new MapLocalRule
        {
            UrlPattern = NewPattern,
            LocalFilePath = NewFilePath,
            StatusCode = NewStatusCode,
            IsEnabled = true
        };

        RuleManager.Instance.AddRule(rule);

        // Reset
        NewPattern = "";
        NewFilePath = "";
    }

    [RelayCommand]
    private void RemoveRule(MapLocalRule rule)
    {
        if (rule == null) return;
        RuleManager.Instance.RemoveRule(rule);
    }

    private static Task ShowAlert(string title, string message)
    {
        var page = Application.Current?.MainPage;
        if (page == null)
            return Task.CompletedTask;

        return page.DisplayAlert(title, message, "OK");
    }
}
