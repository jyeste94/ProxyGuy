using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace ProxyGuy.Helpers;

/// <summary>
/// Persists large request/response bodies to disk so the UI can load them lazily.
/// </summary>
public static class BodyStorage
{
    private static readonly string BasePath = Path.Combine(FileSystem.CacheDirectory, "proxy_bodies");

    public static string SaveBody(long id, string part, string body)
    {
        Directory.CreateDirectory(BasePath);
        var suffix = part.Equals("response", StringComparison.OrdinalIgnoreCase) ? "res" : "req";
        var path = Path.Combine(BasePath, $"{id}_{suffix}.txt");
        File.WriteAllText(path, body);
        return path;
    }

    public static async Task<string> SaveBodyAsync(long id, string part, string body)
    {
        Directory.CreateDirectory(BasePath);
        var suffix = part.Equals("response", StringComparison.OrdinalIgnoreCase) ? "res" : "req";
        var path = Path.Combine(BasePath, $"{id}_{suffix}.txt");
        await File.WriteAllTextAsync(path, body);
        return path;
    }

    public static async Task<string> LoadBodyAsync(string? path, string? fallback = "")
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            try
            {
                return await File.ReadAllTextAsync(path);
            }
            catch
            {
                // Ignore read errors and fall back.
            }
        }

        return fallback ?? string.Empty;
    }

    public static void ClearAll()
    {
        if (!Directory.Exists(BasePath)) return;
        try
        {
            Directory.Delete(BasePath, true);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    public static void CleanupOldFiles(int maxCount = 800)
    {
        if (!Directory.Exists(BasePath)) return;

        try
        {
            var files = new DirectoryInfo(BasePath)
                .GetFiles("*.txt")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .ToList();

            for (int i = maxCount; i < files.Count; i++)
            {
                try { files[i].Delete(); } catch { /* ignore */ }
            }
        }
        catch
        {
            // Swallow any IO errors.
        }
    }
}
