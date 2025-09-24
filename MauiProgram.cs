using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;

namespace ProxyGuy
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    CrashLogger.Log("UnhandledException", ex);
                }
                else
                {
                    CrashLogger.Log("UnhandledException", $"Non-exception: {args.ExceptionObject}");
                }
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                CrashLogger.Log("UnobservedTaskException", args.Exception);
                args.SetObserved();
            };
            return builder.Build();
        }
    }
}


