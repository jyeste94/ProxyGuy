using System;
using System.IO;

namespace ProxyGuyMAUI;

internal static class CrashLogger
{
    public static void Log(string stage, Exception ex)
        => LogInternal(stage, ex.ToString());

    public static void Log(string stage, string message)
        => LogInternal(stage, message);

    private static void LogInternal(string stage, string payload)
    {
        try
        {
            var basePath = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(basePath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
            var path = Path.Combine(basePath, $"diagnostics_{timestamp}.log");
            var lines = new[]
            {
                $"Stage: {stage}",
                payload,
                string.Empty
            };
            File.AppendAllLines(path, lines);
        }
        catch
        {
            // ignore logging errors
        }
    }
}
