using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProxyGuy.Helpers;

public static class PostmanHelper
{
    public static async Task<Dictionary<string, object?>> CreateCollectionAsync(IEnumerable<RequestInfo> infos)
    {
        var items = new List<Dictionary<string, object?>>();
        foreach (var info in infos.OrderBy(i => i.Time))
        {
            items.Add(await CreateItemAsync(info));
        }

        return new Dictionary<string, object?>
        {
            ["info"] = new Dictionary<string, object?>
            {
                ["_postman_id"] = Guid.NewGuid().ToString(),
                ["name"] = $"ProxyGuy Session {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                ["schema"] = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            ["item"] = items
        };
    }

    private static async Task<Dictionary<string, object?>> CreateItemAsync(RequestInfo info)
    {
        var headers = info.RequestHeaders
            .Select(h => new Dictionary<string, object?> { ["key"] = h.Key, ["value"] = h.Value, ["type"] = "text" })
            .ToList<object>();

        var request = new Dictionary<string, object?>
        {
            ["method"] = info.Method,
            ["header"] = headers,
            ["url"] = BuildUrlObject(info.Url)
        };

        var body = await BodyStorage.LoadBodyAsync(info.RequestBodyPath, info.RequestBody);
        if (!string.IsNullOrWhiteSpace(body) && !string.Equals(info.Method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            request["body"] = new Dictionary<string, object?>
            {
                ["mode"] = "raw",
                ["raw"] = body,
                ["options"] = new Dictionary<string, object?>
                {
                    ["raw"] = new Dictionary<string, object?> { ["language"] = GuessLanguage(info.RequestHeaders, body) }
                }
            };
        }

        return new Dictionary<string, object?>
        {
            ["name"] = $"{info.Method} {info.Domain}",
            ["request"] = request,
            ["response"] = Array.Empty<object>()
        };
    }

    private static Dictionary<string, object?> BuildUrlObject(string rawUrl)
    {
        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
        {
            return new Dictionary<string, object?> { ["raw"] = rawUrl };
        }

        var query = new List<Dictionary<string, object?>>();
        var queryString = uri.Query.TrimStart('?');
        if (!string.IsNullOrWhiteSpace(queryString))
        {
            foreach (var pair in queryString.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                query.Add(new Dictionary<string, object?>
                {
                    ["key"] = Uri.UnescapeDataString(parts[0]),
                    ["value"] = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty
                });
            }
        }

        return new Dictionary<string, object?>
        {
            ["raw"] = rawUrl,
            ["protocol"] = uri.Scheme,
            ["host"] = uri.Host.Split('.').Cast<object>().ToArray(),
            ["path"] = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).Cast<object>().ToArray(),
            ["query"] = query
        };
    }

    private static string GuessLanguage(IEnumerable<KeyValuePair<string, string>> headers, string body)
    {
        var contentType = headers.FirstOrDefault(h => h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)).Value;
        if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            return "json";
        if (contentType.Contains("xml", StringComparison.OrdinalIgnoreCase))
            return "xml";
        if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
            return "html";
        if (body.StartsWith("{") || body.StartsWith("["))
            return "json";
        return "text";
    }
}

