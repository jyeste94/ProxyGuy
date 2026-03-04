using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using ProxyGuy.Models;

namespace ProxyGuy.Services;

public class RuleManager
{
    private static readonly RuleManager _instance = new();
    public static RuleManager Instance => _instance;

    public ObservableCollection<MapLocalRule> Rules { get; } = new();

    private RuleManager() { }

    public MapLocalRule? GetMatch(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        return Rules.FirstOrDefault(r => 
            r.IsEnabled && 
            !string.IsNullOrWhiteSpace(r.UrlPattern) && 
            IsMatch(r.UrlPattern, url));
    }

    private bool IsMatch(string pattern, string url)
    {
        // Simple contains check for now, can be upgraded to Regex/Glob
        // If pattern starts with regex:, treat as regex
        // Otherwise simple containment or wildcard
        
        if (pattern.Contains("*"))
        {
            // Simple wildcard support: "api/v1/*" -> "api/v1/"
            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(url, regexPattern, RegexOptions.IgnoreCase);
        }

        return url.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }

    public void AddRule(MapLocalRule rule)
    {
        Rules.Add(rule);
    }

    public void RemoveRule(MapLocalRule rule)
    {
        Rules.Remove(rule);
    }
}
