using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using ProxyGuy;
using ProxyGuy.Models;

namespace ProxyGuy.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ProxyServerService _proxyService;
    private readonly ObservableCollection<RequestItem> _requests = new();
    private readonly Dictionary<int, RequestInfo> _requestLookup = new();
    private readonly HashSet<string> _knownDomains = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;
    private int _nextRequestId = 1;

    public ReadOnlyObservableCollection<RequestItem> Requests { get; }
    public ObservableCollection<RequestItem> VisibleRequests { get; } = new();
    public ObservableCollection<HeaderEntry> RequestHeaders { get; } = new();
    public ObservableCollection<HeaderEntry> ResponseHeaders { get; } = new();
    public ObservableCollection<string> RecentDomains { get; } = new();
    public ObservableCollection<string> VisibleDomains { get; } = new();


    [ObservableProperty]
    private bool isListening = true;

    [ObservableProperty]
    private RequestItem? currentRequest;

    [ObservableProperty]
    private string filterText = string.Empty;

    [ObservableProperty]
    private bool isSidebarCollapsed;

    [ObservableProperty]
    private bool isCompactLayout;

    [ObservableProperty]
    private string? selectedDomain;


    [ObservableProperty]
    private string selectedTab = "Request";

    [ObservableProperty]
    private string statusSummary = "0/0 rows selected";

    [ObservableProperty]
    private string requestBody = string.Empty;

    [ObservableProperty]
    private string responseBody = string.Empty;

    [ObservableProperty]
    private string prettyRequestBody = string.Empty;

    [ObservableProperty]
    private string prettyResponseBody = string.Empty;

    [ObservableProperty]
    private string listeningStatusText = "Proxyguy | Listening on 127.0.0.1:9090";

    public MainViewModel(ProxyServerService proxyService)
    {
        _proxyService = proxyService;
        Requests = new ReadOnlyObservableCollection<RequestItem>(_requests);
        _requests.CollectionChanged += OnRequestsChanged;

        _proxyService.RequestCaptured += OnRequestCaptured;
        _proxyService.RequestsCleared += OnRequestsCleared;

        RecentDomains.CollectionChanged += OnRecentDomainsChanged;
        UpdateVisibleDomains();
        UpdateListeningStatusText(IsListening);
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _initialized = true;

        foreach (var info in _proxyService.CapturedRequests)
        {
            AddRequest(info);
        }

        if (IsListening && !_proxyService.IsRunning)
        {
            await _proxyService.StartAsync();
        }
        else if (!IsListening && _proxyService.IsRunning)
        {
            _proxyService.Stop();
        }

        IsListening = _proxyService.IsRunning;
        RefreshVisibleRequests();
    }


        [RelayCommand]
    private async Task ToggleListening()
    {
        if (_proxyService.IsRunning)
        {
            _proxyService.Stop();
            IsListening = false;
        }
        else
        {
            await _proxyService.StartAsync();
            IsListening = true;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        _proxyService.ClearLogs();
    }

    [RelayCommand]
    private void SelectRequest(RequestItem? item)
    {
        if (item == null)
        {
            return;
        }

        CurrentRequest = item;
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    [RelayCommand]
    private void SelectTab(string tab)
    {
        if (!string.IsNullOrWhiteSpace(tab))
        {
            SelectedTab = tab;
        }
    }

    partial void OnFilterTextChanged(string value)
    {
        UpdateVisibleDomains();
    }

    partial void OnCurrentRequestChanged(RequestItem? value)
    {
        RequestHeaders.Clear();
        ResponseHeaders.Clear();

        if (value != null && _requestLookup.TryGetValue(value.Id, out var info))
        {
            foreach (var header in info.RequestHeaders)
            {
                RequestHeaders.Add(new HeaderEntry { Key = header.Key, Value = header.Value });
            }

            foreach (var header in info.ResponseHeaders)
            {
                ResponseHeaders.Add(new HeaderEntry { Key = header.Key, Value = header.Value });
            }

            RequestBody = info.RequestBody;
            ResponseBody = info.ResponseBody;
            PrettyRequestBody = BeautifyJson(info.RequestBody);
            PrettyResponseBody = BeautifyJson(info.ResponseBody);
        }
        else
        {
            RequestBody = string.Empty;
            ResponseBody = string.Empty;
            PrettyRequestBody = string.Empty;
            PrettyResponseBody = string.Empty;
        }

        UpdateStatusSummary();
    }

    partial void OnIsListeningChanged(bool value)
    {
        UpdateListeningStatusText(value);
    }

    private void OnRequestCaptured(object? sender, RequestInfo info)
    {
        MainThread.BeginInvokeOnMainThread(() => AddRequest(info));
    }

    private void OnRequestsCleared(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _requests.Clear();
            VisibleRequests.Clear();
            _requestLookup.Clear();
            _knownDomains.Clear();
            RecentDomains.Clear();
            _nextRequestId = 1;
            RequestHeaders.Clear();
            ResponseHeaders.Clear();
            RequestBody = string.Empty;
            ResponseBody = string.Empty;
            PrettyRequestBody = string.Empty;
            PrettyResponseBody = string.Empty;
            CurrentRequest = null;
            UpdateStatusSummary();
        });
    }

    private void AddRequest(RequestInfo info)
    {
        var item = MapToRequestItem(info);
        _requestLookup[item.Id] = info;
        _requests.Insert(0, item);
        UpdateDomainList(info.Domain);
        RefreshVisibleRequests();
    }

    private RequestItem MapToRequestItem(RequestInfo info)
    {
        var id = _nextRequestId++;
        var duration = info.Duration.HasValue ? $"{info.Duration.Value.TotalMilliseconds:F0} ms" : "--";

        var statusText = string.IsNullOrWhiteSpace(info.Status)
            ? (info.StatusCode > 0 ? "Completed" : "Pending")
            : info.Status;

        var label = info.IsActive ? "ACTIVE" : string.Empty;
        if (info.StatusCode >= 400)
        {
            label = $"ERR {info.StatusCode}";
        }
        else if (string.Equals(statusText, "Completed", StringComparison.OrdinalIgnoreCase) && info.StatusCode == 304)
        {
            label = "CACHED";
        }

        return new RequestItem
        {
            Id = id,
            Url = info.Url,
            Method = info.Method,
            Status = statusText,
            Code = info.StatusCode > 0 ? info.StatusCode : null,
            Time = info.Time,
            Duration = duration,
            RequestLabel = label,
            IsActive = info.IsActive
        };
    }

    private void UpdateDomainList(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return;
        }

        if (_knownDomains.Add(domain))
        {
            RecentDomains.Insert(0, domain);
            return;
        }

    }

    private void RefreshVisibleRequests()
    {
        var previousSelection = CurrentRequest;

        VisibleRequests.Clear();
        IEnumerable<RequestItem> query = _requests;

        if (!string.IsNullOrWhiteSpace(SelectedDomain))
        {
            query = query.Where(r => string.Equals(r.Host, SelectedDomain, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in query)
        {
            VisibleRequests.Add(item);
        }

        if (VisibleRequests.Count == 0)
        {
            CurrentRequest = null;
        }
        else if (previousSelection == null || !VisibleRequests.Contains(previousSelection))
        {
            CurrentRequest = VisibleRequests.First();
        }
        else
        {
            CurrentRequest = previousSelection;
        }

        UpdateStatusSummary();
    }

    private void UpdateVisibleDomains()
    {
        IEnumerable<string> domains = RecentDomains;

        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            domains = domains.Where(domain => domain.Contains(FilterText, StringComparison.OrdinalIgnoreCase));
        }

        var previousSelection = SelectedDomain;

        VisibleDomains.Clear();
        foreach (var domain in domains)
        {
            VisibleDomains.Add(domain);
        }

        if (!string.IsNullOrEmpty(previousSelection) && !VisibleDomains.Any(domain => string.Equals(domain, previousSelection, StringComparison.OrdinalIgnoreCase)))
        {
            SelectedDomain = null;
        }
    }

    partial void OnSelectedDomainChanged(string? value)
    {
        RefreshVisibleRequests();
    }

    private void OnRecentDomainsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateVisibleDomains();
    }

    private void OnRequestsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateStatusSummary();
    }

    private void UpdateStatusSummary()
    {
        var selectedCount = CurrentRequest != null ? 1 : 0;
        StatusSummary = $"{selectedCount}/{VisibleRequests.Count} rows selected";
    }

    private static string BeautifyJson(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch
        {
            return payload;
        }
    }

    private void UpdateListeningStatusText(bool isListening)
    {
        ListeningStatusText = isListening
            ? "Proxyguy | Listening on 127.0.0.1:9090"
            : "Proxyguy | Paused";
    }
}




