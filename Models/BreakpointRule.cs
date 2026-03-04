using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace ProxyGuy.Models;

public partial class BreakpointRule : ObservableObject
{
    [ObservableProperty]
    private string id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private bool isEnabled = true;

    [ObservableProperty]
    private string urlPattern = string.Empty;

    // For MVP, implies "Request" stage. Could add "Response" later.
}
