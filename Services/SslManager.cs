using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using ProxyGuy.Models;

namespace ProxyGuy.Services;

public class SslManager
{
    private static readonly SslManager _instance = new();
    public static SslManager Instance => _instance;

    public ObservableCollection<SslRule> Rules { get; } = new();

    private SslManager() { }

    public bool ShouldDecrypt(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) return false;

        var activeRules = Rules.Where(r => r.IsEnabled).ToList();
        
        // If no active rules, we assume user wants to capture everything (default behavior)
        if (activeRules.Count == 0)
            return true;

        return activeRules.Any(r => IsMatch(r.HostPattern, host));
    }

    private bool IsMatch(string pattern, string host)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return false;
        
        // Handle wildcards
        // *.google.com should match mail.google.com
        // We escape the pattern but replace \* with .*
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(host, regexPattern, RegexOptions.IgnoreCase);
    }
    
    public void AddRule(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return;

        var normalized = pattern.Trim();
        var exists = Rules.Any(r => string.Equals(r.HostPattern, normalized, StringComparison.OrdinalIgnoreCase));
        if (exists)
            return;

        Rules.Add(new SslRule { HostPattern = normalized });
    }

    public void RemoveRule(SslRule rule)
    {
        Rules.Remove(rule);
    }
}
