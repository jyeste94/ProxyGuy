using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.Input;
using ProxyGuy.Models;

namespace ProxyGuy.ViewModels;

public partial class MainViewModel
{
    [RelayCommand]
    private async Task SaveRequestAsync(RequestItem item)
    {
#if !(WINDOWS || ANDROID || IOS || MACCATALYST)
        _ = item;
        await Task.CompletedTask;
#else
        if (item == null || !_requestLookup.TryGetValue(item.Id, out var info))
        {
            return;
        }

        try
        {
            var payload = new
            {
                info.Time,
                info.Method,
                info.Url,
                info.Domain,
                info.StatusCode,
                info.Status,
                info.IsActive,
                DurationMilliseconds = info.Duration?.TotalMilliseconds,
                info.RequestHeaders,
                info.ResponseHeaders,
                info.RequestBody,
                info.ResponseBody
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            stream.Position = 0;

            var fileName = BuildDefaultFileName(info);
#pragma warning disable CA1416
            var result = await FileSaver.Default.SaveAsync(fileName, stream, CancellationToken.None);
#pragma warning restore CA1416

            if (!result.IsSuccessful && result.Exception != null)
            {
                CrashLogger.Log("SaveRequestAsync", result.Exception);
            }
        }
        catch (Exception ex)
        {
            CrashLogger.Log("SaveRequestAsync", ex);
        }
#endif
    }

    private static string BuildDefaultFileName(RequestInfo info)
    {
        var domain = SanitizeFileName(info.Domain);
        return $"{info.Method}_{domain}_{info.Time:yyyyMMdd_HHmmss}.json";
    }

    private static string SanitizeFileName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "request";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        var sanitized = new string(chars).Trim('_');
        return string.IsNullOrWhiteSpace(sanitized) ? "request" : sanitized;
    }
}
