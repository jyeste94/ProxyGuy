using System;
using System.ComponentModel;
using Microsoft.Maui.ApplicationModel;
using ProxyGuy.ViewModels;
#if WINDOWS
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;
using MauiGridLength = Microsoft.Maui.GridLength;
using MauiGridUnitType = Microsoft.Maui.GridUnitType;
#endif

namespace ProxyGuy;

public partial class MainPage : ContentPage
{
    private readonly ProxyServerService _proxyService;
    private readonly MainViewModel _viewModel;
#if WINDOWS
    private FrameworkElement? _rootElement;
#endif

    public MainPage()
    {
        try
        {
            InitializeComponent();
            _proxyService = new ProxyServerService();
            _viewModel = new MainViewModel(_proxyService);
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.FocusSidebarFilterRequested += OnFocusSidebarFilterRequested;
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
            HookWindowsShortcuts();
#endif
        }
        catch (Exception ex)
        {
            CrashLogger.Log("MainPage.OnAppearing", ex);
            throw;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
#if WINDOWS
        if (_rootElement != null)
        {
            _rootElement.KeyDown -= OnWindowsKeyDown;
            _rootElement = null;
        }
#endif
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        var isCompact = width < 1320;
        var isVeryCompact = width < 1080;
        if (_viewModel.IsCompactLayout != isCompact)
        {
            _viewModel.IsCompactLayout = isCompact;
        }
        if (_viewModel.IsVeryCompactLayout != isVeryCompact)
        {
            _viewModel.IsVeryCompactLayout = isVeryCompact;
        }

        if (MainContentGrid?.RowDefinitions?.Count >= 2)
        {
            if (width < 1150)
            {
                MainContentGrid.RowDefinitions[0].Height = new MauiGridLength(1.45, MauiGridUnitType.Star);
                MainContentGrid.RowDefinitions[1].Height = new MauiGridLength(1, MauiGridUnitType.Star);
            }
            else if (width < 1450)
            {
                MainContentGrid.RowDefinitions[0].Height = new MauiGridLength(1.2, MauiGridUnitType.Star);
                MainContentGrid.RowDefinitions[1].Height = new MauiGridLength(1, MauiGridUnitType.Star);
            }
            else
            {
                MainContentGrid.RowDefinitions[0].Height = new MauiGridLength(1, MauiGridUnitType.Star);
                MainContentGrid.RowDefinitions[1].Height = new MauiGridLength(1.18, MauiGridUnitType.Star);
            }
        }
    }

    private void OnFocusSidebarFilterRequested()
    {
        MainThread.BeginInvokeOnMainThread(() => SidebarControl?.FocusFilter());
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
    private void HookWindowsShortcuts()
    {
        if (_rootElement != null)
            return;

        if (Handler?.PlatformView is FrameworkElement element)
        {
            _rootElement = element;
            _rootElement.KeyDown += OnWindowsKeyDown;
        }
    }

    private void OnWindowsKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var ctrl = IsKeyDown(VirtualKey.Control);
        var shift = IsKeyDown(VirtualKey.Shift);

        if (ctrl && e.Key == VirtualKey.F)
        {
            e.Handled = true;
            _viewModel.FocusSidebarFilterCommand.Execute(null);
            return;
        }

        if (ctrl && shift && e.Key == VirtualKey.D)
        {
            e.Handled = true;
            _viewModel.ToggleRowDensityCommand.Execute(null);
        }
    }

    private static bool IsKeyDown(VirtualKey key)
    {
        var state = InputKeyboardSource.GetKeyStateForCurrentThread(key);
        return (state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
    }

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
