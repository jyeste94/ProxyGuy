using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxyGuy.Models;
using ProxyGuy.Services;

namespace ProxyGuy.ViewModels;

public partial class BreakpointViewModel : ObservableObject
{
    public ObservableCollection<BreakpointRule> Rules => BreakpointManager.Instance.Rules;
    public ObservableCollection<PausedSession> PausedSessions => BreakpointManager.Instance.PausedSessions;

    [ObservableProperty]
    private PausedSession? selectedSession;

    [ObservableProperty]
    private string newPattern = "";

    [RelayCommand]
    private void AddRule()
    {
        if (string.IsNullOrWhiteSpace(NewPattern)) return;
        BreakpointManager.Instance.AddRule(NewPattern);
        NewPattern = string.Empty;
    }

    [RelayCommand]
    private void RemoveRule(BreakpointRule rule)
    {
         if (rule == null) return;
         BreakpointManager.Instance.RemoveRule(rule);
    }

    [RelayCommand]
    private void Resume()
    {
        if (SelectedSession == null) return;
        BreakpointManager.Instance.ResumeSession(SelectedSession);
        SelectedSession = null; // Clear selection
    }

    [RelayCommand]
    private void Abort()
    {
        if (SelectedSession == null) return;
        BreakpointManager.Instance.AbortSession(SelectedSession);
        SelectedSession = null;
    }
}
