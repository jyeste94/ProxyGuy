using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxyGuy.Models;
using ProxyGuy.Helpers;

namespace ProxyGuy.ViewModels;

public partial class RequestComposerViewModel : ObservableObject
{
    private static readonly string[] DefaultMethods = { "GET", "POST", "PUT", "DELETE", "PATCH" };

    public ObservableCollection<HeaderEntry> Headers { get; } = new();

    public IReadOnlyList<string> MethodOptions { get; } = DefaultMethods;

    [ObservableProperty]
    private string url = string.Empty;

    [ObservableProperty]
    private string method = "GET";

    [ObservableProperty]
    private string body = string.Empty;

    [ObservableProperty]
    private string responsePreview = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isSending;

    public async Task LoadFromRequestAsync(RequestInfo info)
    {
        Url = info.Url;
        Method = string.IsNullOrWhiteSpace(info.Method) ? "GET" : info.Method;
        Body = await BodyStorage.LoadBodyAsync(info.RequestBodyPath, info.RequestBody ?? string.Empty);

        Headers.Clear();
        foreach (var h in info.RequestHeaders)
        {
            Headers.Add(new HeaderEntry { Key = h.Key, Value = h.Value });
        }
    }

    [RelayCommand]
    private void AddHeader()
    {
        Headers.Add(new HeaderEntry { Key = "", Value = "" });
    }

    [RelayCommand]
    private void RemoveHeader(HeaderEntry? entry)
    {
        if (entry == null) return;
        Headers.Remove(entry);
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        if (IsSending)
            return;

        if (string.IsNullOrWhiteSpace(Url))
        {
            StatusMessage = "URL requerida";
            return;
        }

        IsSending = true;
        StatusMessage = "Enviando...";
        ResponsePreview = string.Empty;

        try
        {
            var handler = new HttpClientHandler
            {
                Proxy = new System.Net.WebProxy("127.0.0.1", 9090),
                UseProxy = true,
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using var client = new HttpClient(handler);
            using var request = new HttpRequestMessage(new HttpMethod(Method ?? "GET"), Url);

            // Body
            if (!string.IsNullOrEmpty(Body) && !string.Equals(Method, "GET", StringComparison.OrdinalIgnoreCase) && !string.Equals(Method, "HEAD", StringComparison.OrdinalIgnoreCase))
            {
                var contentType = Headers.FirstOrDefault(h => string.Equals(h.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))?.Value;
                if (string.IsNullOrWhiteSpace(contentType) && LooksLikeJson(Body))
                {
                    contentType = "application/json";
                }

                request.Content = new StringContent(Body, Encoding.UTF8, string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
            }

            // Headers
            foreach (var header in Headers)
            {
                if (string.IsNullOrWhiteSpace(header.Key))
                    continue;

                if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value ?? string.Empty))
                {
                    request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value ?? string.Empty);
                }
            }

            var sw = Stopwatch.StartNew();
            var response = await client.SendAsync(request);
            sw.Stop();

            var text = await response.Content.ReadAsStringAsync();
            ResponsePreview = text;
            StatusMessage = $"Respuesta {((int)response.StatusCode)} {response.ReasonPhrase} en {sw.ElapsedMilliseconds} ms";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsSending = false;
        }
    }

    private static bool LooksLikeJson(string payload)
    {
        var trimmed = payload.TrimStart();
        return trimmed.StartsWith("{") || trimmed.StartsWith("[");
    }
}
