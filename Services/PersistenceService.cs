using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using ProxyGuy.Models;

namespace ProxyGuy.Services;

public class AppSettings
{
    public List<MapLocalRule> MapLocalRules { get; set; } = new();
    public List<SslRule> SslRules { get; set; } = new();
    public List<BreakpointRule> BreakpointRules { get; set; } = new();
    public string ThrottleProfileName { get; set; } = "No Throttling";
    public List<string> PhpIniPaths { get; set; } = new();
    public bool IsDenseRows { get; set; }
}

public static class PersistenceService
{
    private static string SettingsPath => Path.Combine(FileSystem.AppDataDirectory, "settings.json");

    public static async Task SaveAsync()
    {
        try
        {
            var settings = new AppSettings
            {
                MapLocalRules = RuleManager.Instance.Rules.ToList(),
                SslRules = SslManager.Instance.Rules.ToList(),
                BreakpointRules = BreakpointManager.Instance.Rules.ToList(),
                ThrottleProfileName = ThrottleManager.Instance.CurrentProfile?.Name ?? "No Throttling",
                PhpIniPaths = PhpIntegrationManager.Instance.PhpIniPaths.ToList(),
                IsDenseRows = UiPreferences.IsDenseRows
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    public static async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;

            var json = await File.ReadAllTextAsync(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);

            if (settings == null) return;

            // Restore Map Local Rules
            RuleManager.Instance.Rules.Clear();
            if (settings.MapLocalRules != null)
            {
                foreach (var r in settings.MapLocalRules) RuleManager.Instance.Rules.Add(r);
            }

            // Restore SSL Rules
            SslManager.Instance.Rules.Clear();
            if (settings.SslRules != null)
            {
                foreach (var r in settings.SslRules) SslManager.Instance.Rules.Add(r);
            }

            // Restore Breakpoint Rules
            BreakpointManager.Instance.Rules.Clear();
            if (settings.BreakpointRules != null)
            {
                foreach (var r in settings.BreakpointRules) BreakpointManager.Instance.Rules.Add(r);
            }
            
            // Restore PHP Integration Paths
            PhpIntegrationManager.Instance.PhpIniPaths.Clear();
            if (settings.PhpIniPaths != null)
            {
                foreach (var p in settings.PhpIniPaths) PhpIntegrationManager.Instance.PhpIniPaths.Add(p);
            }

            UiPreferences.IsDenseRows = settings.IsDenseRows;

            // Restore Throttling

            // Restore Throttling
            if (!string.IsNullOrEmpty(settings.ThrottleProfileName))
            {
                var profile = ThrottleManager.Instance.Profiles.FirstOrDefault(p => p.Name == settings.ThrottleProfileName);
                if (profile != null)
                {
                    ThrottleManager.Instance.CurrentProfile = profile;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }
    }
}
