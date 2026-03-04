using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxyGuy.Models;
using ProxyGuy.Services;

namespace ProxyGuy.ViewModels;

public partial class SslProxyingViewModel : ObservableObject
{
    public ObservableCollection<SslRule> Rules => SslManager.Instance.Rules;

    [ObservableProperty]
    private string newHostPattern = "";

    [RelayCommand]
    private void AddRule()
    {
        if (string.IsNullOrWhiteSpace(NewHostPattern)) return;

        SslManager.Instance.AddRule(NewHostPattern);
        NewHostPattern = string.Empty;
    }

    [RelayCommand]
    private void RemoveRule(SslRule rule)
    {
        if (rule == null) return;
        SslManager.Instance.RemoveRule(rule);
    }
}
