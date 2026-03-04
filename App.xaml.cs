using System;

namespace ProxyGuy
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            CrashLogger.LogInfo("App: Constructor");

            try
            {
                MainPage = new AppShell();
                CrashLogger.LogInfo("App: MainPage assigned");
            }
            catch (Exception ex)
            {
                CrashLogger.Log("App constructor", ex);
                throw;
            }
        }

        protected override async void OnStart()
        {
            base.OnStart();
            CrashLogger.LogInfo("App: OnStart");
            try
            {
                await ProxyGuy.Services.PersistenceService.LoadAsync();
                CrashLogger.LogInfo("App: Persistence Loaded");
            }
            catch (Exception ex)
            {
                CrashLogger.Log("App OnStart", ex);
            }
        }

        protected override async void OnSleep()
        {
            base.OnSleep();
            await ProxyGuy.Services.PersistenceService.SaveAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);

#if WINDOWS
            /*
            window.Created += (s, e) =>
            {
                try
                {
                    var handle = WinRT.Interop.WindowNative.GetWindowHandle(window.Handler.PlatformView);
                    var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);
                    
                    appWindow.Closing += async (sender, args) =>
                    {
                         // ... logic ...
                    };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error hooking window events: {ex}");
                }
            };
            */
#endif
            // Backup save for key events if not intercepted
            window.Destroying += async (s, e) =>
            {
                await ProxyGuy.Services.PersistenceService.SaveAsync();
            };
            return window;
        }
    }
}
