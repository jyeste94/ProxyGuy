using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ProxyGuy.Services;

public class ThrottleProfile
{
    public string Name { get; set; } = string.Empty;
    public int RequestDelayMs { get; set; }
    public int ResponseDelayMs { get; set; }

    public override string ToString() => Name;
}

public partial class ThrottleManager : ObservableObject
{
    private static readonly ThrottleManager _instance = new();
    public static ThrottleManager Instance => _instance;

    public ObservableCollection<ThrottleProfile> Profiles { get; } = new()
    {
        new ThrottleProfile { Name = "No Throttling", RequestDelayMs = 0, ResponseDelayMs = 0 },
        new ThrottleProfile { Name = "Fast 3G (100ms)", RequestDelayMs = 50, ResponseDelayMs = 50 },
        new ThrottleProfile { Name = "Slow 3G (500ms)", RequestDelayMs = 250, ResponseDelayMs = 250 },
        new ThrottleProfile { Name = "Edge / Bad (2s)", RequestDelayMs = 1000, ResponseDelayMs = 1000 },
        new ThrottleProfile { Name = "Very Bad (5s)", RequestDelayMs = 2500, ResponseDelayMs = 2500 }
    };

    [ObservableProperty]
    private ThrottleProfile currentProfile;

    private ThrottleManager()
    {
        CurrentProfile = Profiles[0];
    }

    public async Task DelayRequest()
    {
        if (CurrentProfile != null && CurrentProfile.RequestDelayMs > 0)
        {
            await Task.Delay(CurrentProfile.RequestDelayMs);
        }
    }

    public async Task DelayResponse()
    {
        if (CurrentProfile != null && CurrentProfile.ResponseDelayMs > 0)
        {
            await Task.Delay(CurrentProfile.ResponseDelayMs);
        }
    }
}
