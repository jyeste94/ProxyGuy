using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.Input;
using ProxyGuy.Models;

namespace ProxyGuy.ViewModels;

public partial class MainViewModel
{
    [RelayCommand]
    private async Task SaveSessionPostmanAsync()
    {
#if !(WINDOWS || ANDROID || IOS || MACCATALYST)
        await Task.CompletedTask;
#else
        try
        {
            var infos = VisibleRequests
                .Select(item => _requestLookup.TryGetValue(item.Id, out var info) ? info : null)
                .Where(info => info != null)
                .Cast<RequestInfo>()
                .ToList();

            if (infos.Count == 0)
            {
                return;
            }

            var collection = await ProxyGuy.Helpers.PostmanHelper.CreateCollectionAsync(infos);
            var json = JsonSerializer.Serialize(collection, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            stream.Position = 0;

            var fileName = $"ProxyGuy_Postman_{DateTime.Now:yyyyMMdd_HHmmss}.postman_collection.json";
#pragma warning disable CA1416
            var result = await FileSaver.Default.SaveAsync(fileName, stream, CancellationToken.None);
#pragma warning restore CA1416

            if (result.IsSuccessful)
            {
                var toast = Toast.Make("Collection Postman exportada", ToastDuration.Short);
                await toast.Show();
            }
            else if (result.Exception != null)
            {
                CrashLogger.Log("SaveSessionPostmanAsync", result.Exception);
            }
        }
        catch (Exception ex)
        {
            CrashLogger.Log("SaveSessionPostmanAsync", ex);
        }
#endif
    }

    [RelayCommand]
    private async Task SaveSessionHarAsync()
    {
#if !(WINDOWS || ANDROID || IOS || MACCATALYST)
        await Task.CompletedTask;
#else
        try
        {
            var infos = VisibleRequests
                .Select(item => _requestLookup.TryGetValue(item.Id, out var info) ? info : null)
                .Where(info => info != null)
                .Cast<RequestInfo>()
                .ToList();

            if (infos.Count == 0)
            {
                return;
            }

            var harRoot = await ProxyGuy.Helpers.HarHelper.CreateHarAsync(infos);
            var json = JsonSerializer.Serialize(harRoot, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            stream.Position = 0;

            var fileName = $"ProxyGuy_Session_{DateTime.Now:yyyyMMdd_HHmmss}.har";
#pragma warning disable CA1416
            var result = await FileSaver.Default.SaveAsync(fileName, stream, CancellationToken.None);
#pragma warning restore CA1416

            if (result.IsSuccessful)
            {
                var toast = Toast.Make("Sesion HAR exportada", ToastDuration.Short);
                await toast.Show();
            }
            else if (result.Exception != null)
            {
                CrashLogger.Log("SaveSessionHarAsync", result.Exception);
            }
        }
        catch (Exception ex)
        {
            CrashLogger.Log("SaveSessionHarAsync", ex);
        }
#endif
    }

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
            var harRoot = await ProxyGuy.Helpers.HarHelper.CreateHarAsync(info);
            var json = JsonSerializer.Serialize(harRoot, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

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
        return $"{info.Method}_{domain}_{info.Time:yyyyMMdd_HHmmss}.har";
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
