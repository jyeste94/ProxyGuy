using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;

namespace ProxyGuy
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            CrashLogger.LogInfo("MauiProgram.CreateMauiApp: Starting");
            try 
            {
                var builder = MauiApp.CreateBuilder();
                builder
                    .UseMauiApp<App>()
                    .UseMauiCommunityToolkit()
                    .ConfigureFonts(fonts =>
                    {
                        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                        fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                        fonts.AddFont("Segoe MDL2 Assets.ttf", "Segoe MDL2 Assets");
                    });

#if DEBUG
        		builder.Logging.AddDebug();
#endif
                
                // Services
                builder.Services.AddSingleton<ProxyGuy.ProxyServerService>();
                builder.Services.AddSingleton<ViewModels.MainViewModel>();
                builder.Services.AddSingleton<ViewModels.CertificateViewModel>();
                
                // Views
                builder.Services.AddSingleton<MainPage>();
                builder.Services.AddSingleton<Views.CertificatePage>();

                AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                {
                    if (args.ExceptionObject is Exception ex)
                    {
                        CrashLogger.Log("UnhandledException", ex);
                    }
                    else
                    {
                        CrashLogger.LogInfo($"UnhandledException Non-exception: {args.ExceptionObject}");
                    }
                };

                TaskScheduler.UnobservedTaskException += (sender, args) =>
                {
                    CrashLogger.Log("UnobservedTaskException", args.Exception);
                    args.SetObserved();
                };

                var app = builder.Build();
                CrashLogger.LogInfo("MauiProgram.CreateMauiApp: Build Success");
                return app;
            }
            catch (Exception ex)
            {
                CrashLogger.Log("MauiProgram.CreateMauiApp", ex);
                throw;
            }
        }
    }
}
