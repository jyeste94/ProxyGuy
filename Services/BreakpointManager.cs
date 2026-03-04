using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ProxyGuy.Models;
using Titanium.Web.Proxy.EventArguments;

namespace ProxyGuy.Services;

public class BreakpointManager
{
    private static readonly BreakpointManager _instance = new();
    public static BreakpointManager Instance => _instance;

    public ObservableCollection<BreakpointRule> Rules { get; } = new();
    
    // Sessions currently waiting for user input
    public ObservableCollection<PausedSession> PausedSessions { get; } = new();

    private BreakpointManager() { }

    public bool ShouldIntercept(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        
        var activeRules = Rules.Where(r => r.IsEnabled).ToList();
        return activeRules.Any(r => IsMatch(r.UrlPattern, url));
    }

    private bool IsMatch(string pattern, string url)
    {
         if (string.IsNullOrWhiteSpace(pattern)) return false;
         
         if (pattern.Contains("*"))
         {
             var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
             return Regex.IsMatch(url, regexPattern, RegexOptions.IgnoreCase);
         }
         return url.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }

    public async Task InterceptRequestAsync(SessionEventArgs e)
    {
        // 1. Create a Pause Handle (TCS)
        var tcs = new TaskCompletionSource<bool>();
        
        // 2. Wrap session
        var session = new PausedSession(e, tcs);
        await session.LoadBodyAsync(); // Load body so user can edit it
        
        // 3. Add to UI list (Main Thread required for ObservableCollection?)
        // MAUI ObservableCollections usually should be updated on UI thread 
        // to avoid crashes if bound to UI.
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
        {
            PausedSessions.Add(session);
        });

        // 4. Wait for user action
        await tcs.Task;

        // 5. User action completed (Resume or Abort)
        
        // Remove from list
         Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
        {
            PausedSessions.Remove(session);
        });

        // 6. Apply edits (if Resumed)
        // If aborted, we handle it?
        
        if (tcs.Task.Result) // True = Resume, False = Abort?
        {
             // Apply body changes
             if (e.HttpClient.Request.HasBody && session.RequestBody != null)
             {
                 e.SetRequestBodyString(session.RequestBody);
             }
        }
        else
        {
            // If user clicked Abort, we terminate the request
            e.TerminateSession(); 
        }
    }

    public void ResumeSession(PausedSession session)
    {
        session.CompletionSource.TrySetResult(true);
    }
    
    public void AbortSession(PausedSession session)
    {
        session.CompletionSource.TrySetResult(false);
    }

    public void AddRule(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return;

        var normalized = pattern.Trim();
        var exists = Rules.Any(r => string.Equals(r.UrlPattern, normalized, StringComparison.OrdinalIgnoreCase));
        if (exists)
            return;

        Rules.Add(new BreakpointRule { UrlPattern = normalized });
    }
    
    public void RemoveRule(BreakpointRule rule)
    {
        Rules.Remove(rule);
    }
}
