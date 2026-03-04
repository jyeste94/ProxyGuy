using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace ProxyGuy.Models;

public partial class MapLocalRule : ObservableObject
{
    [ObservableProperty]
    private string id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private bool isEnabled = true;

    [ObservableProperty]
    private string name = "New Rule";

    [ObservableProperty]
    private string urlPattern = string.Empty;

    [ObservableProperty]
    private string localFilePath = string.Empty;

    [ObservableProperty]
    private int statusCode = 200;

    // Could add headers list later
}
