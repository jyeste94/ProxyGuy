using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProxyGuy.Models;

public partial class RequestItem : ObservableObject
{
    public long Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    
    [ObservableProperty]
    private string status = string.Empty;

    [ObservableProperty]
    private int? code;

    [ObservableProperty]
    private string duration = string.Empty;

    [ObservableProperty]
    private string requestLabel = string.Empty;

    [ObservableProperty]
    private bool isActive;

    [ObservableProperty]
    private long responseSizeBytes;

    [ObservableProperty]
    private string contentType = string.Empty;

    public DateTime Time { get; set; }
    public string Host { get; set; } = string.Empty;
}
