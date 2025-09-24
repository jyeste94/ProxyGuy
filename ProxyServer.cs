using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

namespace ProxyGuy;

public class ProxyServerService : INotifyPropertyChanged
{
    private readonly ProxyServer _proxyServer;
    private readonly ExplicitProxyEndPoint _endPoint;
    private readonly Action<string>? _log;
    private long _sequence;

    private string _domainFilter = string.Empty;
    private string? _methodFilter;
    private string? _statusFilter;
    private RequestInfo? _selectedRequest;
    private DomainGroup? _selectedDomain;

    public ObservableCollection<DomainGroup> Domains { get; } = new();
    public ObservableCollection<RequestInfo> FilteredRequests { get; } = new();
    public ObservableCollection<RequestInfo> CapturedRequests { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<RequestInfo>? RequestCaptured;
    public event EventHandler? RequestsCleared;

    public string DomainFilter
    {
        get => _domainFilter;
        set
        {
            if (_domainFilter != value)
            {
                _domainFilter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredDomains));
            }
        }
    }

    public string? MethodFilter
    {
        get => _methodFilter;
        set
        {
            if (_methodFilter != value)
            {
                _methodFilter = value;
                OnPropertyChanged();
                UpdateFilteredRequests();
            }
        }
    }

    public string? StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (_statusFilter != value)
            {
                _statusFilter = value;
                OnPropertyChanged();
                UpdateFilteredRequests();
            }
        }
    }

    public RequestInfo? SelectedRequest
    {
        get => _selectedRequest;
        set
        {
            if (_selectedRequest != value)
            {
                _selectedRequest = value;
                OnPropertyChanged();
            }
        }
    }

    public IReadOnlyList<string> MethodOptions { get; } = new[] { "GET", "POST", "PUT", "DELETE", "PATCH" };
    public IReadOnlyList<string> StatusOptions { get; } = new[] { "200", "301", "302", "400", "401", "403", "404", "500" };

    public Command<RequestInfo> SaveCommand { get; }

    public DomainGroup? SelectedDomain
    {
        get => _selectedDomain;
        set
        {
            if (_selectedDomain != value)
            {
                if (_selectedDomain != null)
                {
                    _selectedDomain.CollectionChanged -= OnSelectedDomainCollectionChanged;
                }

                _selectedDomain = value;

                if (_selectedDomain != null)
                {
                    _selectedDomain.CollectionChanged += OnSelectedDomainCollectionChanged;
                }

                OnPropertyChanged();
                UpdateFilteredRequests();
            }
        }
    }

    public IEnumerable<DomainGroup> FilteredDomains =>
        string.IsNullOrWhiteSpace(DomainFilter)
            ? Domains
            : Domains.Where(d => d.Domain.Contains(DomainFilter, StringComparison.OrdinalIgnoreCase));

    public bool IsRunning => _proxyServer.ProxyRunning;

    public ProxyServerService(Action<string>? logCallback = null)
    {
        _log = logCallback;
        _proxyServer = new ProxyServer();
        _proxyServer.BeforeRequest += OnRequest;
        _proxyServer.BeforeResponse += OnResponse;
        _proxyServer.ForwardToUpstreamGateway = true;
        _endPoint = new ExplicitProxyEndPoint(IPAddress.Loopback, 9090, true);
        SaveCommand = new Command<RequestInfo>(async info =>
        {
            var path = await SaveAsync(info);
            await MainThread.InvokeOnMainThreadAsync(() =>
                Shell.Current.DisplayAlert("Guardado", $"Archivo guardado en {path}", "OK"));
        });
    }

    public void ClearLogs()
    {
        void Execute()
        {
            foreach (var domain in Domains)
            {
                domain.CollectionChanged -= OnSelectedDomainCollectionChanged;
            }

            Domains.Clear();
            SelectedDomain = null;
            SelectedRequest = null;
            FilteredRequests.Clear();
            CapturedRequests.Clear();
            OnPropertyChanged(nameof(FilteredDomains));
            RequestsCleared?.Invoke(this, EventArgs.Empty);
        }

        if (MainThread.IsMainThread)
        {
            Execute();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(Execute);
        }
    }

    public async Task<string> SaveAsync(RequestInfo info)
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, $"request_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        var sb = new StringBuilder();
        sb.AppendLine($"[{info.Time:O}] {info.Method} {info.Url}");
        sb.AppendLine($"Status: {info.StatusCode} ({info.Status})");
        if (info.Duration.HasValue)
        {
            sb.AppendLine($"Duration: {info.Duration.Value.TotalMilliseconds:F0} ms");
        }
        sb.AppendLine("Request Headers:");
        foreach (var h in info.RequestHeaders)
            sb.AppendLine($"{h.Key}: {h.Value}");
        sb.AppendLine();
        if (!string.IsNullOrEmpty(info.RequestBody))
            sb.AppendLine(info.RequestBody);
        sb.AppendLine();
        sb.AppendLine("Response Headers:");
        foreach (var h in info.ResponseHeaders)
            sb.AppendLine($"{h.Key}: {h.Value}");
        sb.AppendLine();
        if (!string.IsNullOrEmpty(info.ResponseBody))
            sb.AppendLine(info.ResponseBody);
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    public Task StartAsync()
    {
        if (_proxyServer.ProxyRunning)
            return Task.CompletedTask;

        _proxyServer.AddEndPoint(_endPoint);
        _proxyServer.Start();
        OnPropertyChanged(nameof(IsRunning));
        return Task.CompletedTask;
    }

    public void Stop()
    {
        if (!_proxyServer.ProxyRunning)
            return;

        _proxyServer.Stop();
        OnPropertyChanged(nameof(IsRunning));
    }

    private async Task OnRequest(object sender, SessionEventArgs e)
    {
        var info = new RequestInfo
        {
            Sequence = Interlocked.Increment(ref _sequence),
            Method = e.HttpClient.Request.Method,
            Url = e.HttpClient.Request.Url,
            Domain = new Uri(e.HttpClient.Request.Url).Host,
            Time = DateTime.Now,
            Status = "Active",
            IsActive = true
        };

        foreach (var header in e.HttpClient.Request.Headers)
        {
            info.RequestHeaders.Add(new(header.Name, header.Value));
        }

        if (e.HttpClient.Request.HasBody)
        {
            e.HttpClient.Request.KeepBody = true;
            try
            {
                info.RequestBody = await e.GetRequestBodyAsString();
            }
            catch
            {
                info.RequestBody = string.Empty;
            }
        }

        e.UserData = info;
        _log?.Invoke($"REQ: {info.Method} {info.Url}");
    }

    private async Task OnResponse(object sender, SessionEventArgs e)
    {
        var info = e.UserData as RequestInfo ?? new RequestInfo
        {
            Sequence = Interlocked.Increment(ref _sequence),
            Method = e.HttpClient.Request.Method,
            Url = e.HttpClient.Request.Url,
            Domain = new Uri(e.HttpClient.Request.Url).Host,
            Time = DateTime.Now
        };

        if (!info.RequestHeaders.Any())
        {
            foreach (var header in e.HttpClient.Request.Headers)
            {
                info.RequestHeaders.Add(new(header.Name, header.Value));
            }
        }

        foreach (var header in e.HttpClient.Response.Headers)
        {
            info.ResponseHeaders.Add(new(header.Name, header.Value));
        }

        info.StatusCode = e.HttpClient.Response.StatusCode;
        info.Status = info.StatusCode >= 400 ? "Error" : "Completed";
        info.IsActive = false;
        info.CompletedAt = DateTime.Now;
        info.Duration = info.CompletedAt - info.Time;

        if (e.HttpClient.Response.HasBody)
        {
            e.HttpClient.Response.KeepBody = true;
            try
            {
                info.ResponseBody = await e.GetResponseBodyAsString();
            }
            catch (Exception ex)
            {
                info.ResponseBody = $"<error reading body: {ex.Message}>";
            }
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var group = Domains.FirstOrDefault(g => g.Domain == info.Domain);
            if (group == null)
            {
                group = new DomainGroup(info.Domain);
                Domains.Add(group);
                OnPropertyChanged(nameof(FilteredDomains));
            }

            group.Insert(0, info);

            if (group == SelectedDomain)
            {
                UpdateFilteredRequests();
            }

            if (CapturedRequests.Count >= 1000)
            {
                CapturedRequests.RemoveAt(CapturedRequests.Count - 1);
            }

            CapturedRequests.Insert(0, info);
            RequestCaptured?.Invoke(this, info);
        });

        _log?.Invoke($"RES: {e.HttpClient.Response.StatusCode} {e.HttpClient.Request.Url}");
    }

    private void OnSelectedDomainCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateFilteredRequests();
    }

    private void UpdateFilteredRequests()
    {
        void Refresh()
        {
            FilteredRequests.Clear();

            if (SelectedDomain == null)
            {
                SelectedRequest = null;
                return;
            }

            var matching = SelectedDomain.Where(r =>
                (string.IsNullOrEmpty(MethodFilter) || string.Equals(r.Method, MethodFilter, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(StatusFilter) || r.StatusCode.ToString() == StatusFilter));

            foreach (var item in matching)
            {
                FilteredRequests.Add(item);
            }

            if (FilteredRequests.Count == 0)
            {
                SelectedRequest = null;
                return;
            }

            if (SelectedRequest == null || !FilteredRequests.Contains(SelectedRequest))
            {
                SelectedRequest = FilteredRequests.First();
            }
        }

        if (MainThread.IsMainThread)
        {
            Refresh();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(Refresh);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}