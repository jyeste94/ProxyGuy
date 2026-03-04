using CommunityToolkit.Mvvm.ComponentModel;

namespace ProxyGuy.Models;

public partial class DomainEntry : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private int count;
}
