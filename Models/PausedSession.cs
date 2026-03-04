using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Titanium.Web.Proxy.EventArguments;

namespace ProxyGuy.Models;

public partial class PausedSession : ObservableObject
{
    public SessionEventArgs SessionArgs { get; }
    public TaskCompletionSource<bool> CompletionSource { get; }

    [ObservableProperty]
    private string url;

    [ObservableProperty]
    private string method;
    
    // Editable Properties
    [ObservableProperty]
    private string requestBody = string.Empty;

    // We might need editable headers list too, but let's start with Body for MVP
    
    public PausedSession(SessionEventArgs args, TaskCompletionSource<bool> tcs)
    {
        SessionArgs = args;
        CompletionSource = tcs;
        Url = args.HttpClient.Request.Url;
        Method = args.HttpClient.Request.Method;
        
        if (args.HttpClient.Request.HasBody)
        {
             // Note: Reading body here might be tricky if not fully read yet.
             // We rely on 'GetRequestBodyAsString'
        }
    }

    public async Task LoadBodyAsync()
    {
        if (SessionArgs.HttpClient.Request.HasBody)
        {
            RequestBody = await SessionArgs.GetRequestBodyAsString() ?? string.Empty;
        }
    }
}
