using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using ProxyGuy.Helpers;

namespace ProxyGuy;

public class ProxyServerService
{
    private readonly ProxyServer _proxyServer;
    private readonly ExplicitProxyEndPoint _endPoint;
    private readonly Action<string>? _log;
    private const int MaxBodyChars = 200_000; // guard to avoid UI freezes on huge payloads
    private const int InlineBodyChars = 8_000; // inline small payloads, persist larger ones to disk
    private const long MaxCapturedBodyBytes = 512 * 1024; // avoid heavy payload allocations
    private long _sequence;
    private bool _endPointAdded;

    public event EventHandler<RequestInfo>? RequestCaptured;
    public event EventHandler? ProxyStarted;
    public event EventHandler? ProxyStopped;
    
    public static ProxyServerService Instance { get; private set; }

    public bool IsRunning => _proxyServer.ProxyRunning;

    public ProxyServerService(Action<string>? logCallback = null)
    {
        Instance = this;
        _log = logCallback;
        _proxyServer = new ProxyServer();
        _proxyServer.BeforeRequest += OnRequest;
        _proxyServer.BeforeResponse += OnResponse;
        
        // Optimize: Don't decrypt SSL by default for everything if not needed, 
        // but for a debugger we usually want it. 
        // We can add logic to only decrypt domains we care about later.
        _proxyServer.CertificateManager.RootCertificateIssuerName = "ProxyGuy Root CA";
        _proxyServer.CertificateManager.RootCertificateName = "ProxyGuy Root CA"; 
        // Ensure we store it in AppData so we can find it
        var certPath = Path.Combine(FileSystem.AppDataDirectory, "rootCert.pfx");
        
        if (File.Exists(certPath))
        {
             _proxyServer.CertificateManager.RootCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath, string.Empty, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.Exportable);
        }
        else
        {
             _proxyServer.CertificateManager.CreateRootCertificate();
             if (_proxyServer.CertificateManager.RootCertificate != null)
             {
                 File.WriteAllBytes(certPath, _proxyServer.CertificateManager.RootCertificate.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pfx, string.Empty));
             }
        }

        // Export PEM for PHP
        var pemPath = Path.Combine(FileSystem.AppDataDirectory, "rootCert.pem");
        if (_proxyServer.CertificateManager.RootCertificate != null)
        {
             var cert = _proxyServer.CertificateManager.RootCertificate;
             var pem = "-----BEGIN CERTIFICATE-----\r\n" + 
                       Convert.ToBase64String(cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks) + 
                       "\r\n-----END CERTIFICATE-----";
             File.WriteAllText(pemPath, pem);
        }

        _proxyServer.ForwardToUpstreamGateway = true;
        
        // Listen on Loopback
        _endPoint = new ExplicitProxyEndPoint(IPAddress.Loopback, 9090, true);
        
        // SSL Proxying List Logic
        _endPoint.BeforeTunnelConnectRequest += async (sender, e) =>
        {
            string hostname = e.WebSession.Request.RequestUri.Host;
            if (!ProxyGuy.Services.SslManager.Instance.ShouldDecrypt(hostname))
            {
                e.DecryptSsl = false;
            }
            await Task.CompletedTask;
        };
    }

    public Task StartAsync()
    {
        if (_proxyServer.ProxyRunning)
            return Task.CompletedTask;

        if (!_endPointAdded)
        {
            _proxyServer.AddEndPoint(_endPoint);
            _endPointAdded = true;
        }
        _proxyServer.Start();
        
        // Set as System Proxy
        _proxyServer.SetAsSystemHttpProxy(_endPoint);
        _proxyServer.SetAsSystemHttpsProxy(_endPoint);
        
        // PHP Integration
        ProxyGuy.Services.PhpIntegrationManager.Instance.StartIntegration();
        
        ProxyStarted?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public void Stop()
    {
        if (!_proxyServer.ProxyRunning)
            return;

        // Clear System Proxy
        _proxyServer.DisableSystemHttpProxy();
        _proxyServer.DisableSystemHttpsProxy();
        
        // PHP Integration Restore
        ProxyGuy.Services.PhpIntegrationManager.Instance.StopIntegration();

        _proxyServer.Stop();
        ProxyStopped?.Invoke(this, EventArgs.Empty);
    }

    private async Task OnRequest(object sender, SessionEventArgs e)
    {
        // Network Throttling (Request)
        await ProxyGuy.Services.ThrottleManager.Instance.DelayRequest();

        // 1. Capture basic info
        var info = new RequestInfo
        {
            Sequence = Interlocked.Increment(ref _sequence),
            Method = e.HttpClient.Request.Method,
            Url = e.HttpClient.Request.Url,
            Domain = GetHostSafe(e.HttpClient.Request.Url),
            Time = DateTime.Now,
            Status = "Active",
            IsActive = true
        };

        // Check for Map Local Rule
        var rule = ProxyGuy.Services.RuleManager.Instance.GetMatch(info.Url);
        if (rule != null && File.Exists(rule.LocalFilePath))
        {
            try
            {
                var bodyBytes = await File.ReadAllBytesAsync(rule.LocalFilePath);
                
                // Serve Mock Response
                var headers = new Dictionary<string, HttpHeader>();
                headers.Add("Content-Type", new HttpHeader("Content-Type", "application/json")); // Default, should be configurable or inferred
                headers.Add("X-ProxyGuy-Mock", new HttpHeader("X-ProxyGuy-Mock", "true"));
                headers.Add("X-ProxyGuy-Mock-Status", new HttpHeader("X-ProxyGuy-Mock-Status", rule.StatusCode.ToString()));

                e.Ok(bodyBytes, headers, true);
                if (e.HttpClient?.Response != null)
                {
                    e.HttpClient.Response.StatusCode = rule.StatusCode;
                }

                // Update info to reflect mock
                info.Status = "Mocked";
                info.StatusCode = rule.StatusCode;
                info.ResponseBody = "(Local File Content)"; // Or load it?
                info.ResponseSizeBytes = bodyBytes.Length;
                info.IsActive = false; // It completes immediately
                info.ResponseStartedAt = DateTime.Now;
                info.CompletedAt = DateTime.Now;
                info.Duration = info.CompletedAt - info.Time;
                
                _log?.Invoke($"MOCK: {info.Url} -> {rule.LocalFilePath}");
                
                // We still want headers/body in UI?
                // `e.Ok` stops the request from going upstream.
                // We should ensure we populate RequestInfo enough for UI.
                
                // Populate request headers as usual so we see what was sent
                foreach (var header in e.HttpClient.Request.Headers)
                {
                    info.RequestHeaders.Add(new(header.Name, header.Value));
                }
                
                // Populate response headers (mocked)
                info.ResponseHeaders.Add(new("Content-Type", "application/json"));
                info.ResponseHeaders.Add(new("X-ProxyGuy-Mock", "true"));
                
                e.UserData = info;
                RequestCaptured?.Invoke(this, info);
                return; 
            }
            catch (Exception ex)
            {
                _log?.Invoke($"MOCK ERROR: {ex.Message}");
            }
        }

        // 2. Headers
        foreach (var header in e.HttpClient.Request.Headers)
        {
            info.RequestHeaders.Add(new(header.Name, header.Value));
        }

        var requestContentType = GetHeaderValue(info.RequestHeaders, "Content-Type");
        var requestContentLength = ParseContentLength(GetHeaderValue(info.RequestHeaders, "Content-Length"));

        // 3. Body
        if (e.HttpClient.Request.HasBody)
        {
            if (!ShouldCaptureBody(requestContentType, requestContentLength))
            {
                info.RequestBody = BuildOmittedBodyMessage(requestContentType, requestContentLength);
            }
            else
            {
                e.HttpClient.Request.KeepBody = true;
                try
                {
                    var body = TruncateBody(await e.GetRequestBodyAsString());
                    if (string.IsNullOrEmpty(body))
                    {
                        info.RequestBody = string.Empty;
                    }
                    else if (body.Length <= InlineBodyChars)
                    {
                        info.RequestBody = body;
                    }
                    else
                    {
                        info.RequestBodyPath = await SaveBodySafeAsync(info.Sequence, "request", body);
                        if (string.IsNullOrWhiteSpace(info.RequestBodyPath))
                        {
                            info.RequestBody = body;
                        }
                    }
                }
                catch
                {
                    info.RequestBody = string.Empty;
                }
            }
        }

        // Breakpoints (Interception)
        if (ProxyGuy.Services.BreakpointManager.Instance.ShouldIntercept(info.Url))
        {
             _log?.Invoke($"PAUSED: {info.Url}");
             info.Status = "Paused"; // Update status for UI
             RequestCaptured?.Invoke(this, info); // Notify UI it's paused
             
             await ProxyGuy.Services.BreakpointManager.Instance.InterceptRequestAsync(e);
             
             // After Resume
             info.Status = "Resumed"; 
             _log?.Invoke($"RESUMED: {info.Url}");
        }

        e.UserData = info;
        _log?.Invoke($"REQ: {info.Method} {info.Url}");
        
        // Notify UI of new request (Active)
        RequestCaptured?.Invoke(this, info);
    }

    private async Task OnResponse(object sender, SessionEventArgs e)
    {
        // Network Throttling (Response)
        await ProxyGuy.Services.ThrottleManager.Instance.DelayResponse();

        var info = e.UserData as RequestInfo;
        if (info == null)
        {
           // Should not happen usually if OnRequest was called
           return;
        }

        info.ResponseStartedAt ??= DateTime.Now;

        // Fill Response Info
        if (!info.RequestHeaders.Any())
        {
             // Sometimes request headers might need refresh if not captured early?
             foreach (var header in e.HttpClient.Request.Headers)
             {
                 info.RequestHeaders.Add(new(header.Name, header.Value));
             }
        }

        foreach (var header in e.HttpClient.Response.Headers)
        {
            info.ResponseHeaders.Add(new(header.Name, header.Value));
        }

        var responseContentType = GetHeaderValue(info.ResponseHeaders, "Content-Type");
        var responseContentLength = ParseContentLength(GetHeaderValue(info.ResponseHeaders, "Content-Length"));
        if (responseContentLength.HasValue && responseContentLength.Value > 0)
        {
            info.ResponseSizeBytes = responseContentLength.Value;
        }

        info.StatusCode = e.HttpClient.Response.StatusCode;
        info.Status = info.StatusCode >= 400 ? "Error" : "Completed";
        info.IsActive = false;
        info.CompletedAt = DateTime.Now;
        info.Duration = info.CompletedAt - info.Time;

        if (e.HttpClient.Response.HasBody)
        {
            if (!ShouldCaptureBody(responseContentType, responseContentLength))
            {
                info.ResponseBody = BuildOmittedBodyMessage(responseContentType, responseContentLength);
            }
            else
            {
                e.HttpClient.Response.KeepBody = true;
                try
                {
                    var body = TruncateBody(await e.GetResponseBodyAsString());
                    if (string.IsNullOrEmpty(body))
                    {
                        info.ResponseBody = string.Empty;
                        info.ResponseSizeBytes = 0;
                    }
                    else if (body.Length <= InlineBodyChars)
                    {
                        info.ResponseBody = body;
                        info.ResponseSizeBytes = body.Length;
                    }
                    else
                    {
                        info.ResponseBodyPath = await SaveBodySafeAsync(info.Sequence, "response", body);
                        info.ResponseSizeBytes = body.Length;
                        if (string.IsNullOrWhiteSpace(info.ResponseBodyPath))
                        {
                            info.ResponseBody = body;
                        }
                    }
                }
                catch (Exception ex)
                {
                    info.ResponseBody = $"<error reading body: {ex.Message}>";
                }
            }
        }

        _log?.Invoke($"RES: {e.HttpClient.Response.StatusCode} {e.HttpClient.Request.Url}");
        
        // Notify UI of update (Completed)
        // Note: In Titanium Proxy, the SAME object instance is passed through. 
        // The View Model should simply trigger a PropertyChanged or refresh for this item.
        // Or we can fire the event again to signal 'Update'.
        RequestCaptured?.Invoke(this, info);
    }

    private string TruncateBody(string body)
    {
        if (string.IsNullOrEmpty(body))
            return body;

        if (body.Length <= MaxBodyChars)
            return body;

        return body.Substring(0, MaxBodyChars) + $"... [truncated {body.Length - MaxBodyChars} chars]";
    }

    private static string GetHostSafe(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
            return uri.Host;

        return "unknown-host";
    }

    private static async Task<string?> SaveBodySafeAsync(long sequence, string part, string body)
    {
        try
        {
            return await BodyStorage.SaveBodyAsync(sequence, part, body);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetHeaderValue(IEnumerable<KeyValuePair<string, string>> headers, string key)
    {
        foreach (var header in headers)
        {
            if (string.Equals(header.Key, key, StringComparison.OrdinalIgnoreCase))
                return header.Value;
        }

        return null;
    }

    private static long? ParseContentLength(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var token = value.Split(',')[0].Trim();
        if (long.TryParse(token, out var bytes) && bytes >= 0)
            return bytes;

        return null;
    }

    private static bool ShouldCaptureBody(string? contentType, long? contentLength)
    {
        if (contentLength.HasValue && contentLength.Value > MaxCapturedBodyBytes)
            return false;

        if (string.IsNullOrWhiteSpace(contentType))
            return true;

        var type = contentType.ToLowerInvariant();
        if (type.StartsWith("image/") ||
            type.StartsWith("video/") ||
            type.StartsWith("audio/") ||
            type.StartsWith("font/"))
        {
            return false;
        }

        if (type.Contains("application/octet-stream") ||
            type.Contains("application/pdf") ||
            type.Contains("application/zip") ||
            type.Contains("application/gzip") ||
            type.Contains("application/x-protobuf") ||
            type.Contains("multipart/form-data"))
        {
            return false;
        }

        return true;
    }

    private static string BuildOmittedBodyMessage(string? contentType, long? contentLength)
    {
        var type = string.IsNullOrWhiteSpace(contentType) ? "unknown" : contentType;
        var size = contentLength.HasValue ? $"{contentLength.Value} bytes" : "unknown size";
        return $"<body omitted: {type}, {size}>";
    }
}
