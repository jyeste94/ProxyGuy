using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;

namespace ProxyGuy.ViewModels;

public partial class MainViewModel
{
    [RelayCommand]
    private async Task NavigateToMapLocal()
    {
        await Shell.Current.GoToAsync("MapLocalPage");
    }

    [RelayCommand]
    private async Task NavigateToCertificates()
    {
        await Shell.Current.GoToAsync("CertificatePage");
    }

    [RelayCommand]
    private async Task NavigateToSslProxying()
    {
        await Shell.Current.GoToAsync("SslProxyingPage");
    }

    [RelayCommand]
    private async Task NavigateToBreakpoints()
    {
        await Shell.Current.GoToAsync("BreakpointPage");
    }

    [RelayCommand]
    private async Task NavigateToPhpIntegration()
    {
        await Shell.Current.GoToAsync("PhpIntegrationPage");
    }

    [RelayCommand]
    private void SetThrottleProfile(string? profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            return;

        var profile = ProxyGuy.Services.ThrottleManager.Instance.Profiles
            .FirstOrDefault(p => string.Equals(p.Name, profileName, StringComparison.OrdinalIgnoreCase));
        if (profile != null)
        {
            ProxyGuy.Services.ThrottleManager.Instance.CurrentProfile = profile;
        }
    }
}
