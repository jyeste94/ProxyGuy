using System;
using System.ComponentModel;
using ProxyGuyMAUI.ViewModels;

namespace ProxyGuyMAUI;

public partial class MainPage : ContentPage
{
    private readonly ProxyServerService _proxyService;
    private readonly MainViewModel _viewModel;

    public MainPage()
    {
        try
        {
            InitializeComponent();
            _proxyService = new ProxyServerService();
            _viewModel = new MainViewModel(_proxyService);
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            BindingContext = _viewModel;
        }
        catch (Exception ex)
        {
            CrashLogger.Log("MainPage ctor", ex);
            throw;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _viewModel.InitializeAsync();
#if WINDOWS
            UpdateWindowsProxy(_viewModel.IsListening);
#endif
        }
        catch (Exception ex)
        {
            CrashLogger.Log("MainPage.OnAppearing", ex);
            throw;
        }
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        var isCompact = width < 1200;
        if (_viewModel.IsCompactLayout != isCompact)
        {
            _viewModel.IsCompactLayout = isCompact;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
#if WINDOWS
        if (string.Equals(e.PropertyName, nameof(MainViewModel.IsListening), StringComparison.Ordinal))
        {
            UpdateWindowsProxy(_viewModel.IsListening);
        }
#else
        _ = sender;
        _ = e;
#endif
    }

#if WINDOWS
    private static void UpdateWindowsProxy(bool enabled)
    {
        if (enabled)
        {
            WindowsProxyHelper.Enable("127.0.0.1", 9090);
        }
        else
        {
            WindowsProxyHelper.Disable();
        }
    }
#endif
}
