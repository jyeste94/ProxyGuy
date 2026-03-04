using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace ProxyGuy.Models;

public partial class SslRule : ObservableObject
{
    [ObservableProperty]
    private string id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private bool isEnabled = true;

    [ObservableProperty]
    private string hostPattern = string.Empty; // e.g. *.google.com, api.example.com
}
