using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using ProxyGuy;
using ProxyGuy.Models;
using ProxyGuy.Helpers;

namespace ProxyGuy.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ProxyServerService _proxyService;
    public ProxyGuy.Services.RuleManager RuleManager => ProxyGuy.Services.RuleManager.Instance;
    public ProxyGuy.Services.ThrottleManager ThrottleManager => ProxyGuy.Services.ThrottleManager.Instance;
    private readonly ObservableCollection<RequestItem> _requests = new();
    private readonly Dictionary<long, RequestItem> _requestItems = new();
    private readonly Dictionary<long, RequestInfo> _requestLookup = new(); // Key is RequestInfo.Sequence
    private readonly HashSet<string> _knownDomains = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DomainEntry> _domainEntries = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;
    
    private const int MaxTrackedRequests = 500;
    private const int MaxPrettyChars = 200_000;
    private const int MaxDomainEntries = 200;
    private const int PageSize = 100;
    private int _visibleLimit = PageSize;

    private bool _operationInProgress;
    private bool _detailsLoading;
    private bool _isBusy;
    private CancellationTokenSource? _detailsCts;
    private int _refreshVersion;
    private int _prettyVersion;
    private bool _isUpdatingDomains;
    private bool _restoringSelectedDomain;
    private string? _selectedDomainKey;
    private int _domainRefreshScheduled;
    private int _captureFlushScheduled;
    private bool _captureFlushInProgress;
    private bool _suppressStatusSummary;
    private readonly Queue<RequestInfo> _capturedQueue = new();
    private readonly object _capturedQueueLock = new();

    private const int CaptureFlushDelayMs = 33;
    private const int CaptureBatchSize = 180;
    private const int MaxPendingCaptureEvents = 4000;
    private const int MaxClipboardChars = 120_000;

    public event Action? FocusSidebarFilterRequested;

    public ReadOnlyObservableCollection<RequestItem> Requests { get; }
    public ObservableRangeCollection<RequestItem> VisibleRequests { get; } = new();
    public ObservableCollection<HeaderEntry> RequestHeaders { get; } = new();
    public ObservableCollection<HeaderEntry> ResponseHeaders { get; } = new();
    public ObservableCollection<string> RecentDomains { get; } = new();
    public ObservableCollection<DomainEntry> VisibleDomains { get; } = new();

    public IReadOnlyList<string> MethodOptions { get; } = new[] { "GET", "POST", "PUT", "DELETE", "PATCH" };
    public IReadOnlyList<string> StatusOptions { get; } = new[] { "200", "301", "302", "400", "401", "403", "404", "500" };
    public IReadOnlyList<string> TimeRangeOptions { get; } = new[] { "All", "Last 5m", "Last 15m", "Last 1h", "Today" };

    [ObservableProperty]
    private bool isListening = true;

    [ObservableProperty]
    private RequestItem? currentRequest;

    [ObservableProperty]
    private string filterText = string.Empty;
    
    [ObservableProperty]
    private string? methodFilter;

    [ObservableProperty]
    private string? statusFilter;

    [ObservableProperty]
    private bool isSidebarCollapsed;

    [ObservableProperty]
    private bool isCompactLayout;

    [ObservableProperty]
    private bool isVeryCompactLayout;

    [ObservableProperty]
    private bool isDenseRows;

    [ObservableProperty]
    private DomainEntry? selectedDomain;

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

    [ObservableProperty]
    private string urlFilter = string.Empty;

    [ObservableProperty]
    private bool useRegexFilter;

    [ObservableProperty]
    private string contentTypeFilter = string.Empty;

    [ObservableProperty]
    private string selectedTimeRange = "All";

    [ObservableProperty]
    private string minResponseKb = string.Empty;

    [ObservableProperty]
    private string maxResponseKb = string.Empty;

    [ObservableProperty]
    private bool hasMoreItems;

    [ObservableProperty]
    private string detailSearchText = string.Empty;

    [ObservableProperty]
    private string detailSearchSummary = string.Empty;

    [ObservableProperty]
    private double selectedTtfbMs;

    [ObservableProperty]
    private double selectedTransferMs;

    [ObservableProperty]
    private double selectedTotalMs;

    [ObservableProperty]
    private double selectedTtfbRatio;

    [ObservableProperty]
    private double selectedTransferRatio;

    public string RowDensityText => IsDenseRows ? "Density: Compact" : "Density: Comfortable";

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public MainViewModel(ProxyServerService proxyService)
    {
        _proxyService = proxyService;
        Requests = new ReadOnlyObservableCollection<RequestItem>(_requests);
        _requests.CollectionChanged += OnRequestsChanged;

        _proxyService.RequestCaptured += OnRequestCaptured;
        
        UpdateVisibleDomains();
        UpdateListeningStatusText(IsListening);
        IsDenseRows = ProxyGuy.Services.UiPreferences.IsDenseRows;
        UpdateBusyState();
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _initialized = true;
        SetOperationInProgress(true);

        try
        {
            if (IsListening && !_proxyService.IsRunning)
            {
                await _proxyService.StartAsync();
            }
            else if (!IsListening && _proxyService.IsRunning)
            {
                _proxyService.Stop();
            }

            IsListening = _proxyService.IsRunning;
        }
        finally
        {
            SetOperationInProgress(false);
        }
    }

    [RelayCommand]
    private void ExitApplication()
    {
        Application.Current?.Quit();
    }

    [RelayCommand]
    private async Task ToggleListening()
    {
        SetOperationInProgress(true);

        try
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
        finally
        {
            SetOperationInProgress(false);
        }
    }

    [RelayCommand]
    private void Clear()
    {
        CancelDetailsLoad();
        Interlocked.Increment(ref _prettyVersion);
        _visibleLimit = PageSize;
        Interlocked.Exchange(ref _domainRefreshScheduled, 0);
        Interlocked.Exchange(ref _captureFlushScheduled, 0);
        _captureFlushInProgress = false;
        lock (_capturedQueueLock)
        {
            _capturedQueue.Clear();
        }
        
        // Clear local state
        lock (_requests)
        {
            _requests.Clear();
            _requestItems.Clear();
            _requestLookup.Clear();
        }
        VisibleRequests.Clear();
        _knownDomains.Clear();
        _domainEntries.Clear();
        RecentDomains.Clear();
        VisibleDomains.Clear();
        HasMoreItems = false;
        
        RequestHeaders.Clear();
        ResponseHeaders.Clear();
        RequestBody = string.Empty;
        ResponseBody = string.Empty;
        PrettyRequestBody = string.Empty;
        PrettyResponseBody = string.Empty;
        DetailSearchText = string.Empty;
        DetailSearchSummary = string.Empty;
        SelectedTtfbMs = 0;
        SelectedTransferMs = 0;
        SelectedTotalMs = 0;
        SelectedTtfbRatio = 0;
        SelectedTransferRatio = 0;
        CurrentRequest = null;
        _selectedDomainKey = null;
        SelectedDomain = null;
        BodyStorage.ClearAll();
        
        UpdateStatusSummary();
        SetDetailLoading(false);
    }
    
    // SaveRequest removed - implemented in MainViewModel.Save.cs

    [RelayCommand]
    private void SelectRequest(RequestItem? item)
    {
        if (item == null)
            return;

        CurrentRequest = item;
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    [RelayCommand]
    private void ToggleRowDensity()
    {
        IsDenseRows = !IsDenseRows;
    }

    [RelayCommand]
    private void FocusSidebarFilter()
    {
        FocusSidebarFilterRequested?.Invoke();
    }

    [RelayCommand]
    private void LoadMoreRequests()
    {
        var newLimit = Math.Min(_visibleLimit + PageSize, MaxTrackedRequests);
        if (newLimit == _visibleLimit)
            return;

        _visibleLimit = newLimit;
        RefreshVisibleRequests();
    }

    [RelayCommand]
    private void SelectTab(string tab)
    {
        if (!string.IsNullOrWhiteSpace(tab))
        {
            SelectedTab = tab;
        }
    }

    [RelayCommand]
    private async Task ShowCertificate()
    {
        try
        {
            CrashLogger.Log("ShowCertificate", "Button clicked - Starting navigation");

            var navigation = Application.Current?.MainPage?.Navigation;
            if (navigation == null)
                return;

            var certificatePage = new ProxyGuy.Views.CertificatePage();
            await navigation.PushModalAsync(certificatePage);
            
            CrashLogger.Log("ShowCertificate", "Navigation completed");
        }
        catch (Exception ex)
        {
            CrashLogger.Log("MainViewModel.ShowCertificate", ex);
        }
    }

    [RelayCommand]
    private async Task OpenComposer()
    {
        if (CurrentRequest == null)
            return;

        if (!_requestLookup.TryGetValue(CurrentRequest.Id, out var info))
            return;

        try
        {
            var navigation = Application.Current?.MainPage?.Navigation;
            if (navigation == null)
                return;

            var composerVm = new RequestComposerViewModel();
            await composerVm.LoadFromRequestAsync(info);

            var page = new ProxyGuy.Views.RequestComposerPage(composerVm);
            await navigation.PushModalAsync(page);
        }
        catch (Exception ex)
        {
            CrashLogger.Log("MainViewModel.OpenComposer", ex);
        }
    }

    [RelayCommand]
    private async Task CopyCurl()
    {
        if (CurrentRequest == null)
            return;

        if (!_requestLookup.TryGetValue(CurrentRequest.Id, out var info))
            return;

        try
        {
            var curl = CurlHelper.GenerateCurl(info);
            if (string.IsNullOrWhiteSpace(curl))
                return;

            await Clipboard.SetTextAsync(curl);
            var toast = Toast.Make("Copiado al portapapeles", ToastDuration.Short);
            await toast.Show();
        }
        catch (Exception ex)
        {
            CrashLogger.Log("MainViewModel.CopyCurl", ex);
        }
    }

    [RelayCommand]
    private async Task CopyRequestBody()
    {
        if (CurrentRequest == null) return;
        if (!_requestLookup.TryGetValue(CurrentRequest.Id, out var info)) return;

        var text = await BodyStorage.LoadBodyAsync(info.RequestBodyPath, info.RequestBody);
        await CopyTextAsync(text, "Body de request copiado");
    }

    [RelayCommand]
    private async Task CopyResponseBody()
    {
        if (CurrentRequest == null) return;
        if (!_requestLookup.TryGetValue(CurrentRequest.Id, out var info)) return;

        var text = await BodyStorage.LoadBodyAsync(info.ResponseBodyPath, info.ResponseBody);
        await CopyTextAsync(text, "Body de response copiado");
    }

    private static async Task CopyTextAsync(string text, string toastMessage)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var truncated = false;
        if (text.Length > MaxClipboardChars)
        {
            text = text[..MaxClipboardChars];
            truncated = true;
        }

        await Clipboard.SetTextAsync(text);
        var message = truncated ? $"{toastMessage} (recortado por tamano)" : toastMessage;
        var toast = Toast.Make(message, ToastDuration.Short);
        await toast.Show();
    }

    partial void OnFilterTextChanged(string value) => UpdateVisibleDomains();
    
    partial void OnMethodFilterChanged(string? value)
    {
        _visibleLimit = PageSize;
        RefreshVisibleRequests();
    }
    
    partial void OnStatusFilterChanged(string? value)
    {
        _visibleLimit = PageSize;
        RefreshVisibleRequests();
    }

    partial void OnUrlFilterChanged(string value)
    {
        _visibleLimit = PageSize;
        RefreshVisibleRequests();
    }

    partial void OnUseRegexFilterChanged(bool value)
    {
        _visibleLimit = PageSize;
        RefreshVisibleRequests();
    }

    partial void OnContentTypeFilterChanged(string value)
    {
        _visibleLimit = PageSize;
        RefreshVisibleRequests();
    }

    partial void OnSelectedTimeRangeChanged(string value)
    {
        _visibleLimit = PageSize;
        RefreshVisibleRequests();
    }

    partial void OnMinResponseKbChanged(string value)
    {
        _visibleLimit = PageSize;
        RefreshVisibleRequests();
    }

    partial void OnMaxResponseKbChanged(string value)
    {
        _visibleLimit = PageSize;
        RefreshVisibleRequests();
    }

    partial void OnDetailSearchTextChanged(string value)
    {
        UpdateDetailSearchSummary();
    }

    partial void OnSelectedTabChanged(string value)
    {
        if (string.Equals(value, "Pretty", StringComparison.OrdinalIgnoreCase))
        {
            EnsurePrettyBodiesForCurrentRequest();
        }
    }

    partial void OnIsDenseRowsChanged(bool value)
    {
        ProxyGuy.Services.UiPreferences.IsDenseRows = value;
        OnPropertyChanged(nameof(RowDensityText));
    }

    partial void OnCurrentRequestChanged(RequestItem? value)
    {
        CancelDetailsLoad();
        Interlocked.Increment(ref _prettyVersion);

        if (value == null || !_requestLookup.TryGetValue(value.Id, out var info))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RequestHeaders.Clear();
                ResponseHeaders.Clear();
                RequestBody = string.Empty;
                ResponseBody = string.Empty;
                PrettyRequestBody = string.Empty;
                PrettyResponseBody = string.Empty;
                DetailSearchSummary = string.Empty;
                SelectedTtfbMs = 0;
                SelectedTransferMs = 0;
                SelectedTotalMs = 0;
                SelectedTtfbRatio = 0;
                SelectedTransferRatio = 0;
                UpdateStatusSummary();
                SetDetailLoading(false);
            });
            return;
        }

        var cts = new CancellationTokenSource();
        _detailsCts = cts;
        SetDetailLoading(true);

        Task.Run(async () =>
        {
            try
            {
                cts.Token.ThrowIfCancellationRequested();

                var requestHeaders = info.RequestHeaders
                    .Select(header => new HeaderEntry { Key = header.Key, Value = header.Value })
                    .ToList();

                var responseHeaders = info.ResponseHeaders
                    .Select(header => new HeaderEntry { Key = header.Key, Value = header.Value })
                    .ToList();

                var requestBody = await BodyStorage.LoadBodyAsync(info.RequestBodyPath, info.RequestBody);
                var responseBody = await BodyStorage.LoadBodyAsync(info.ResponseBodyPath, info.ResponseBody);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (cts.IsCancellationRequested || _detailsCts != cts)
                        return;

                    RequestHeaders.Clear();
                    foreach (var header in requestHeaders) RequestHeaders.Add(header);

                    ResponseHeaders.Clear();
                    foreach (var header in responseHeaders) ResponseHeaders.Add(header);

                    RequestBody = requestBody;
                    ResponseBody = responseBody;
                    PrettyRequestBody = string.Empty;
                    PrettyResponseBody = string.Empty;
                    UpdateTimingMetrics(info);
                    UpdateDetailSearchSummary();
                    if (string.Equals(SelectedTab, "Pretty", StringComparison.OrdinalIgnoreCase))
                    {
                        EnsurePrettyBodiesForCurrentRequest();
                    }
                    UpdateStatusSummary();
                    SetDetailLoading(false);
                });
            }
            catch (OperationCanceledException)
            {
                // Selection changed before details completed.
            }
            catch (Exception ex)
            {
                CrashLogger.Log("MainViewModel.LoadRequestDetails", ex);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (_detailsCts != cts) return;
                    SetDetailLoading(false);
                });
            }
            finally
            {
                if (ReferenceEquals(_detailsCts, cts))
                    CancelDetailsLoad();
            }
        }, cts.Token);
    }

    partial void OnIsListeningChanged(bool value)
    {
        UpdateListeningStatusText(value);
    }

    private void OnRequestCaptured(object? sender, RequestInfo info)
    {
        lock (_capturedQueueLock)
        {
            if (_capturedQueue.Count >= MaxPendingCaptureEvents)
            {
                _capturedQueue.Dequeue();
            }
            _capturedQueue.Enqueue(info);
        }

        ScheduleCapturedFlush();
    }

    private void ScheduleCapturedFlush()
    {
        if (Interlocked.Exchange(ref _captureFlushScheduled, 1) == 1)
        {
            return;
        }

        Task.Run(async () =>
        {
            await Task.Delay(CaptureFlushDelayMs).ConfigureAwait(false);
            MainThread.BeginInvokeOnMainThread(FlushCapturedQueue);
        });
    }

    private void FlushCapturedQueue()
    {
        if (_captureFlushInProgress)
            return;

        _captureFlushInProgress = true;
        Interlocked.Exchange(ref _captureFlushScheduled, 0);

        try
        {
            var batch = new List<RequestInfo>(CaptureBatchSize);
            lock (_capturedQueueLock)
            {
                while (_capturedQueue.Count > 0 && batch.Count < CaptureBatchSize)
                {
                    batch.Add(_capturedQueue.Dequeue());
                }
            }

            if (batch.Count == 0)
                return;

            var coalescedOrder = new List<long>(batch.Count);
            var coalesced = new Dictionary<long, RequestInfo>(batch.Count);
            foreach (var info in batch)
            {
                if (!coalesced.ContainsKey(info.Sequence))
                {
                    coalescedOrder.Add(info.Sequence);
                }
                coalesced[info.Sequence] = info;
            }

            _suppressStatusSummary = true;
            try
            {
                foreach (var sequence in coalescedOrder)
                {
                    try
                    {
                        AddOrUpdateRequest(coalesced[sequence]);
                    }
                    catch (Exception ex)
                    {
                        CrashLogger.Log("MainViewModel.FlushCapturedQueue", ex);
                    }
                }
            }
            finally
            {
                _suppressStatusSummary = false;
            }

            UpdateStatusSummary();
        }
        finally
        {
            _captureFlushInProgress = false;
        }

        bool hasMore;
        lock (_capturedQueueLock)
        {
            hasMore = _capturedQueue.Count > 0;
        }

        if (hasMore)
        {
            ScheduleCapturedFlush();
        }
    }

    private void AddOrUpdateRequest(RequestInfo info)
    {
        _requestLookup[info.Sequence] = info;

        if (_requestItems.TryGetValue(info.Sequence, out var existingItem))
        {
            UpdateItemFromInfo(existingItem, info);

            var shouldDisplay = ShouldDisplay(existingItem);
            var existingVisibleIndex = VisibleRequests.IndexOf(existingItem);
            if (shouldDisplay && existingVisibleIndex < 0)
            {
                AddToVisible(existingItem);
            }
            else if (!shouldDisplay && existingVisibleIndex >= 0)
            {
                VisibleRequests.RemoveAt(existingVisibleIndex);
            }
            
            if (CurrentRequest == existingItem && !info.IsActive)
            {
                OnCurrentRequestChanged(existingItem);
            }
        }
        else
        {
            var item = MapToRequestItem(info);
            lock (_requests)
            {
                _requestItems[info.Sequence] = item;
                _requests.Insert(0, item);
                IncrementDomainCount(info.Domain);
                TrimRequestCollections();
            }

            if (ShouldDisplay(item))
            {
                AddToVisible(item);
            }

            HasMoreItems = _requests.Count > _visibleLimit;
            // EnsureCurrentRequestIsVisible(); // Disabled auto-selection
        }
    }

    private void UpdateItemFromInfo(RequestItem item, RequestInfo info)
    {
        var duration = info.Duration.HasValue ? $"{info.Duration.Value.TotalMilliseconds:F0} ms" : "--";
        var statusText = string.IsNullOrWhiteSpace(info.Status)
            ? (info.StatusCode > 0 ? "Completed" : "Pending")
            : info.Status;
            
        var label = info.IsActive ? "ACTIVE" : string.Empty;
        if (info.StatusCode >= 400)
            label = $"ERR {info.StatusCode}";
        else if (string.Equals(statusText, "Completed", StringComparison.OrdinalIgnoreCase) && info.StatusCode == 304)
            label = "CACHED";
            
        item.Status = statusText;
        item.Code = info.StatusCode > 0 ? info.StatusCode : null;
        item.Duration = duration;
        item.RequestLabel = label;
        item.IsActive = info.IsActive;
        item.ResponseSizeBytes = info.ResponseSizeBytes;
        item.ContentType = ExtractContentType(info);
    }

    private void TrimRequestCollections()
    {
        // Assumes lock(_requests) is held by caller if called from AddOrUpdateRequest
        // But for safety if called elsewhere:
        // Actually since it's private and only called from AddOrUpdateRequest which locks, is it fine?
        // AddOrUpdateRequest takes the lock. If I lock again it's reentrant (Monitor supports this).
        // Best practice: lock here too just in case.
        // Wait, if I assume caller locks, I should document it.
        // But AddOrUpdateRequest calls it inside lock.
        // Let's rely on reentrancy of Monitor (lock) which is standard in C#.
        lock (_requests)
        {
            while (_requests.Count > MaxTrackedRequests)
            {
                var removed = _requests[^1];
                _requests.RemoveAt(_requests.Count - 1);
                _requestLookup.Remove(removed.Id);
                _requestItems.Remove(removed.Id);
                DecrementDomainCount(removed.Host);
                VisibleRequests.Remove(removed);
            }
        }
    }

    private void TrimVisibleRequests()
    {
        var limit = Math.Min(_visibleLimit, MaxTrackedRequests);
        while (VisibleRequests.Count > limit)
        {
            if (SelectedDomain != null)
            {
                // Remove oldest when over limit
                VisibleRequests.RemoveAt(0);
            }
            else
            {
                VisibleRequests.RemoveAt(VisibleRequests.Count - 1);
            }
        }
    }

    private void AddToVisible(RequestItem item)
    {
        if (SelectedDomain != null)
        {
            VisibleRequests.Add(item); // append to preserve scroll position when filtering by domain
        }
        else
        {
            VisibleRequests.Insert(0, item);
        }

        TrimVisibleRequests();
    }

    /* 
    // Auto-selection removed
    private void EnsureCurrentRequestIsVisible()
    {
        if (CurrentRequest != null && !VisibleRequests.Contains(CurrentRequest))
        {
            CurrentRequest = VisibleRequests.FirstOrDefault();
        }
        else if (CurrentRequest == null && VisibleRequests.Count > 0)
        {
            CurrentRequest = VisibleRequests[0];
        }
    }
    */

    private bool ShouldDisplay(RequestItem item)
    {
        var now = DateTime.Now;
        var minBytes = ParseKbToBytes(MinResponseKb);
        var maxBytes = ParseKbToBytes(MaxResponseKb);
        var lowerBound = ResolveLowerBound(SelectedTimeRange, now);

        return ShouldDisplay(
            item,
            SelectedDomain?.Name,
            MethodFilter,
            StatusFilter,
            UrlFilter,
            UseRegexFilter,
            ContentTypeFilter,
            lowerBound,
            minBytes,
            maxBytes);
    }

    private static bool ShouldDisplay(
        RequestItem item,
        string? selectedDomainName,
        string? methodFilter,
        string? statusFilter,
        string? urlFilter,
        bool useRegexFilter,
        string? contentTypeFilter,
        DateTime? lowerBound,
        long? minResponseBytes,
        long? maxResponseBytes)
    {
        if (!string.IsNullOrWhiteSpace(selectedDomainName) &&
            !string.Equals(item.Host, selectedDomainName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(methodFilter) &&
            !string.Equals(item.Method, methodFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(statusFilter))
        {
             if (item.Code == null || item.Code.ToString() != statusFilter)
                return false;
        }

        if (!string.IsNullOrWhiteSpace(urlFilter))
        {
            var filterText = urlFilter;
            var exclude = filterText.StartsWith("!", StringComparison.Ordinal);
            if (exclude && filterText.Length > 1)
            {
                filterText = filterText[1..];
            }

            bool match;
            if (useRegexFilter)
            {
                try
                {
                    match = Regex.IsMatch(item.Url ?? string.Empty, filterText, RegexOptions.IgnoreCase);
                }
                catch
                {
                    match = true; // invalid regex: do not block items
                }
            }
            else
            {
                match = item.Url != null && item.Url.Contains(filterText, StringComparison.OrdinalIgnoreCase);
            }

            if (!exclude && !match)
                return false;

            if (exclude && match)
                return false;
        }

        if (!string.IsNullOrWhiteSpace(contentTypeFilter))
        {
            if (string.IsNullOrWhiteSpace(item.ContentType) ||
                !item.ContentType.Contains(contentTypeFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (lowerBound.HasValue && item.Time < lowerBound.Value)
        {
            return false;
        }

        if (minResponseBytes.HasValue && item.ResponseSizeBytes < minResponseBytes.Value)
        {
            return false;
        }

        if (maxResponseBytes.HasValue && item.ResponseSizeBytes > maxResponseBytes.Value)
        {
            return false;
        }

        return true;
    }

    private RequestItem MapToRequestItem(RequestInfo info)
    {
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
            Id = info.Sequence,
            Url = info.Url,
            Method = info.Method,
            Status = statusText,
            Code = info.StatusCode > 0 ? info.StatusCode : null,
            Time = info.Time,
            Duration = duration,
            RequestLabel = label,
            IsActive = info.IsActive,
            ResponseSizeBytes = info.ResponseSizeBytes,
            ContentType = ExtractContentType(info),
            Host = info.Domain
        };
    }

    private void TouchDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return;

        if (_knownDomains.Add(domain))
        {
            RecentDomains.Insert(0, domain);
            while (RecentDomains.Count > MaxDomainEntries)
            {
                var removedDomain = RecentDomains[^1];
                RecentDomains.RemoveAt(RecentDomains.Count - 1);
                _knownDomains.Remove(removedDomain);
                _domainEntries.Remove(removedDomain);
            }
            return;
        }
        // Keep existing domain position stable to avoid selection churn in UI.
    }

    private void IncrementDomainCount(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return;

        if (!_domainEntries.TryGetValue(domain, out var entry))
        {
            entry = new DomainEntry { Name = domain, Count = 0 };
            _domainEntries[domain] = entry;
        }
        entry.Count++;

        TouchDomain(domain);
        QueueDomainRefresh();
    }

    private void DecrementDomainCount(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return;

        if (_domainEntries.TryGetValue(domain, out var entry))
        {
            if (entry.Count <= 1)
            {
                _domainEntries.Remove(domain);
                _knownDomains.Remove(domain);
                RecentDomains.Remove(domain);
            }
            else
            {
                entry.Count--;
            }
        }

        QueueDomainRefresh();
    }

    private void RefreshVisibleRequests()
    {
        var previousSelection = CurrentRequest;
        var limit = Math.Min(_visibleLimit, MaxTrackedRequests);
        var refreshVersion = Interlocked.Increment(ref _refreshVersion);
        var selectedDomainName = SelectedDomain?.Name;
        var methodFilter = MethodFilter;
        var statusFilter = StatusFilter;
        var urlFilter = UrlFilter;
        var useRegex = UseRegexFilter;
        var contentTypeFilter = ContentTypeFilter;
        var lowerBound = ResolveLowerBound(SelectedTimeRange, DateTime.Now);
        var minBytes = ParseKbToBytes(MinResponseKb);
        var maxBytes = ParseKbToBytes(MaxResponseKb);
        
        // Run filtering on background thread with snapshot filters.
        Task.Run(() =>
        {
            var newVisible = new List<RequestItem>();
            var hasMore = false;
            RequestItem[] snapshot;
            lock (_requests)
            {
               snapshot = _requests.ToArray();
            }

            var enumerable = selectedDomainName == null ? snapshot.AsEnumerable() : snapshot.Reverse();

            foreach (var item in enumerable)
            {
                if (ShouldDisplay(item, selectedDomainName, methodFilter, statusFilter, urlFilter, useRegex, contentTypeFilter, lowerBound, minBytes, maxBytes))
                {
                    if (newVisible.Count < limit)
                    {
                        newVisible.Add(item);
                    }
                    else
                    {
                        hasMore = true;
                        break;
                    }
                }
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (refreshVersion != _refreshVersion)
                    return;

                VisibleRequests.ReplaceRange(newVisible);
                HasMoreItems = hasMore;

                if (VisibleRequests.Count == 0)
                {
                    CurrentRequest = null;
                }
                else if (previousSelection != null && VisibleRequests.Contains(previousSelection))
                {
                    CurrentRequest = previousSelection;
                }
                else
                {
                    CurrentRequest = null; // Do not auto-select first item
                }

                UpdateStatusSummary();
            });
        });
    }

    private void QueueDomainRefresh()
    {
        if (Interlocked.Exchange(ref _domainRefreshScheduled, 1) == 1)
        {
            return;
        }

        Task.Run(async () =>
        {
            await Task.Delay(120).ConfigureAwait(false);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Interlocked.Exchange(ref _domainRefreshScheduled, 0);
                UpdateVisibleDomains();
            });
        });
    }

    private void UpdateVisibleDomains()
    {
        _isUpdatingDomains = true;
        try
        {
            IEnumerable<string> sourceDomains = RecentDomains;

            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                sourceDomains = sourceDomains.Where(domain => domain.Contains(FilterText, StringComparison.OrdinalIgnoreCase));
            }

            var targetList = sourceDomains
                .Where(domain => _domainEntries.ContainsKey(domain))
                .Select(domain => _domainEntries[domain])
                .ToList();
            var toRemove = new List<DomainEntry>();

            foreach (var item in VisibleDomains)
            {
                if (!targetList.Any(d => string.Equals(d.Name, item.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    toRemove.Add(item);
                }
            }

            foreach (var item in toRemove)
            {
                VisibleDomains.Remove(item);
            }

            for (int i = 0; i < targetList.Count; i++)
            {
                var item = targetList[i];
                var existingIndex = -1;
                for (int j = 0; j < VisibleDomains.Count; j++)
                {
                    if (string.Equals(VisibleDomains[j].Name, item.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        existingIndex = j;
                        break;
                    }
                }

                if (existingIndex < 0)
                {
                    VisibleDomains.Insert(i, item);
                }
                else
                {
                    var existing = VisibleDomains[existingIndex];
                    if (existing.Count != item.Count)
                    {
                        existing.Count = item.Count;
                    }

                    if (existingIndex != i)
                    {
                        VisibleDomains.Move(existingIndex, i);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(_selectedDomainKey))
            {
                var match = VisibleDomains.FirstOrDefault(d => string.Equals(d.Name, _selectedDomainKey, StringComparison.OrdinalIgnoreCase));
                if (match != null && !ReferenceEquals(SelectedDomain, match))
                {
                    _restoringSelectedDomain = true;
                    SelectedDomain = match;
                    _restoringSelectedDomain = false;
                }
                else if (match == null)
                {
                    _selectedDomainKey = null;
                    if (SelectedDomain != null)
                    {
                        _restoringSelectedDomain = true;
                        SelectedDomain = null;
                        _restoringSelectedDomain = false;
                    }
                }
            }
        }
        finally
        {
            _isUpdatingDomains = false;
        }
    }

    partial void OnSelectedDomainChanged(DomainEntry? value)
    {
        if (_restoringSelectedDomain)
            return;

        if (_isUpdatingDomains)
        {
            if (value != null)
            {
                _selectedDomainKey = value.Name;
            }
            return;
        }

        _selectedDomainKey = value?.Name;

        _visibleLimit = PageSize;
        RefreshVisibleRequests();
    }
    
    private void OnRequestsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_suppressStatusSummary)
            return;

        UpdateStatusSummary();
    }

    private void UpdateStatusSummary()
    {
        var selectedCount = CurrentRequest != null ? 1 : 0;
        StatusSummary = $"{selectedCount} sel | {VisibleRequests.Count} visibles | {_requests.Count} total";
    }

    private static string ExtractContentType(RequestInfo info)
    {
        foreach (var header in info.ResponseHeaders)
        {
            if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                return header.Value ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static long? ParseKbToBytes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().Replace(',', '.');
        if (!double.TryParse(normalized, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var kb))
            return null;

        if (kb < 0)
            return null;

        return (long)Math.Round(kb * 1024d);
    }

    private static DateTime? ResolveLowerBound(string? selectedTimeRange, DateTime now)
    {
        return selectedTimeRange switch
        {
            "Last 5m" => now.AddMinutes(-5),
            "Last 15m" => now.AddMinutes(-15),
            "Last 1h" => now.AddHours(-1),
            "Today" => now.Date,
            _ => null
        };
    }

    private void UpdateDetailSearchSummary()
    {
        if (string.IsNullOrWhiteSpace(DetailSearchText))
        {
            DetailSearchSummary = string.Empty;
            return;
        }

        var term = DetailSearchText.Trim();
        var reqBodyMatches = CountMatches(RequestBody, term);
        var resBodyMatches = CountMatches(ResponseBody, term);
        var reqHeaderMatches = RequestHeaders.Count(h =>
            (!string.IsNullOrEmpty(h.Key) && h.Key.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(h.Value) && h.Value.Contains(term, StringComparison.OrdinalIgnoreCase)));
        var resHeaderMatches = ResponseHeaders.Count(h =>
            (!string.IsNullOrEmpty(h.Key) && h.Key.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(h.Value) && h.Value.Contains(term, StringComparison.OrdinalIgnoreCase)));

        var total = reqBodyMatches + resBodyMatches + reqHeaderMatches + resHeaderMatches;
        if (total == 0)
        {
            DetailSearchSummary = "No matches";
            return;
        }

        DetailSearchSummary = $"ReqB:{reqBodyMatches} ResB:{resBodyMatches} ReqH:{reqHeaderMatches} ResH:{resHeaderMatches}";
    }

    private static int CountMatches(string? source, string term)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(term))
            return 0;

        var count = 0;
        var idx = 0;
        while (true)
        {
            idx = source.IndexOf(term, idx, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return count;

            count++;
            idx += term.Length;
        }
    }

    private void EnsurePrettyBodiesForCurrentRequest()
    {
        if (CurrentRequest == null)
            return;

        if (!string.IsNullOrEmpty(PrettyRequestBody) || !string.IsNullOrEmpty(PrettyResponseBody))
            return;

        var requestId = CurrentRequest.Id;
        var requestBodySnapshot = RequestBody;
        var responseBodySnapshot = ResponseBody;
        var version = Interlocked.Increment(ref _prettyVersion);

        Task.Run(() =>
        {
            var prettyRequest = BeautifyJson(requestBodySnapshot);
            var prettyResponse = BeautifyJson(responseBodySnapshot);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (version != _prettyVersion)
                    return;

                if (CurrentRequest == null || CurrentRequest.Id != requestId)
                    return;

                PrettyRequestBody = prettyRequest;
                PrettyResponseBody = prettyResponse;
            });
        });
    }

    private void UpdateTimingMetrics(RequestInfo info)
    {
        var total = Math.Max(0, info.Duration?.TotalMilliseconds ?? 0);
        var ttfb = 0d;
        if (info.ResponseStartedAt.HasValue)
        {
            ttfb = Math.Max(0, (info.ResponseStartedAt.Value - info.Time).TotalMilliseconds);
            if (ttfb > total && total > 0)
            {
                ttfb = total;
            }
        }

        var transfer = Math.Max(0, total - ttfb);

        SelectedTotalMs = total;
        SelectedTtfbMs = ttfb;
        SelectedTransferMs = transfer;

        if (total <= 0)
        {
            SelectedTtfbRatio = 0;
            SelectedTransferRatio = 0;
            return;
        }

        SelectedTtfbRatio = Math.Clamp(ttfb / total, 0, 1);
        SelectedTransferRatio = Math.Clamp(transfer / total, 0, 1);
    }

    private static string BeautifyJson(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return string.Empty;
        if (payload.Length > MaxPrettyChars)
        {
            return payload;
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

    private void SetOperationInProgress(bool isRunning)
    {
        if (_operationInProgress == isRunning) return;
        _operationInProgress = isRunning;
        UpdateBusyState();
    }
    
    private void UpdateBusyState()
    {
        IsBusy = _operationInProgress || _detailsLoading;
    }

    private void UpdateListeningStatusText(bool isListening)
    {
        ListeningStatusText = isListening
            ? "Proxyguy | Listening on 127.0.0.1:9090"
            : "Proxyguy | Paused";
    }

    private void CancelDetailsLoad()
    {
        var existing = Interlocked.Exchange(ref _detailsCts, null);
        if (existing == null) return;
        existing.Cancel();
        existing.Dispose();
    }

    private void SetDetailLoading(bool isLoading)
    {
        if (_detailsLoading == isLoading) return;
        _detailsLoading = isLoading;
        UpdateBusyState();
    }
}

