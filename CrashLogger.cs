using System;
using System.IO;
using Microsoft.Maui.Storage;

namespace ProxyGuy;

public static class CrashLogger
{
    public static void Log(string context, Exception ex)
    {
        try
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, "crash_log.txt");
            var message = $"[{DateTime.Now}] CRASH in {context}: {ex}\n\n";
            File.AppendAllText(path, message);
        }
        catch
        {
            // Fallback
        }
    }

    public static void LogInfo(string message)
    {
         try
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, "crash_log.txt");
            File.AppendAllText(path, $"[{DateTime.Now}] INFO: {message}\n");
        }
        catch { }
    }

    public static void Log(string context, string message)
    {
        LogInfo($"{context}: {message}");
    }
}
