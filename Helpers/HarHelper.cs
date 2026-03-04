using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProxyGuy.Models;
using ProxyGuy.Models.Har;

namespace ProxyGuy.Helpers;

public static class HarHelper
{
    public static async Task<HarRoot> CreateHarAsync(RequestInfo info)
    {
        return await CreateHarAsync(new[] { info });
    }

    public static async Task<HarRoot> CreateHarAsync(IEnumerable<RequestInfo> infos)
    {
        var root = new HarRoot();
        foreach (var info in infos.OrderBy(i => i.Time))
        {
            root.Log.Entries.Add(await CreateEntryAsync(info));
        }

        return root;
    }

    private static async Task<HarEntry> CreateEntryAsync(RequestInfo info)
    {
        return new HarEntry
        {
            StartedDateTime = info.Time.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            Time = info.Duration?.TotalMilliseconds ?? 0,
            Request = await CreateRequestAsync(info),
            Response = await CreateResponseAsync(info),
            Cache = new HarCache(),
            Timings = new HarTimings
            {
                // We don't track granular timings yet, so everything is 'wait'
                Wait = info.Duration?.TotalMilliseconds ?? 0
            }
        };
    }

    private static async Task<HarRequest> CreateRequestAsync(RequestInfo info)
    {
        var req = new HarRequest
        {
            Method = info.Method,
            Url = info.Url,
            HttpVersion = "HTTP/1.1" // We assume 1.1 for now
        };

        if (Uri.TryCreate(info.Url, UriKind.Absolute, out var requestUri))
        {
            var query = requestUri.Query;
            if (!string.IsNullOrWhiteSpace(query))
            {
                foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = pair.Split('=', 2);
                    var name = Uri.UnescapeDataString(parts[0]);
                    var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
                    req.QueryString.Add(new HarQueryParam { Name = name, Value = value });
                }
            }
        }

        // Headers
        req.Headers = info.RequestHeaders
            .Select(h => new HarHeader { Name = h.Key, Value = h.Value })
            .ToList();

        // Cookies (simple parsing)
        // Ideally we parse Cookie header
        var cookieHeader = info.RequestHeaders.FirstOrDefault(h => h.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase));
        if (cookieHeader.Key != null) 
        {
             // Simple split, not full RFC compliance
             var parts = cookieHeader.Value.Split(';');
             foreach(var part in parts)
             {
                 var kv = part.Trim().Split('=');
                 if (kv.Length == 2)
                 {
                     req.Cookies.Add(new HarCookie { Name = kv[0], Value = kv[1] });
                 }
             }
        }

        // Body
        var body = await BodyStorage.LoadBodyAsync(info.RequestBodyPath, info.RequestBody);
        if (!string.IsNullOrEmpty(body))
        {
            req.PostData = new HarPostData
            {
                MimeType = req.Headers.FirstOrDefault(h => h.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))?.Value ?? "text/plain",
                Text = body
            };
            req.BodySize = body.Length;
        }
        else
        {
            req.BodySize = 0;
        }

        req.HeadersSize = -1; // Not calculating exact raw header size

        return req;
    }

    private static async Task<HarResponse> CreateResponseAsync(RequestInfo info)
    {
        var res = new HarResponse
        {
            Status = info.StatusCode,
            StatusText = info.Status ?? "",
            HttpVersion = "HTTP/1.1"
        };

        // Headers
        res.Headers = info.ResponseHeaders
            .Select(h => new HarHeader { Name = h.Key, Value = h.Value })
            .ToList();

        // Cookies (simple parsing from Set-Cookie)
        var setCookies = info.ResponseHeaders.Where(h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));
        foreach (var sc in setCookies)
        {
             // Very naive parsing: just taking name=value part before first semicolon
             var firstPart = sc.Value.Split(';').FirstOrDefault();
             if (firstPart != null)
             {
                 var kv = firstPart.Trim().Split('=');
                 if (kv.Length >= 2)
                 {
                      res.Cookies.Add(new HarCookie { Name = kv[0], Value = kv[1] });
                 }
             }
        }

        // Body
        var body = await BodyStorage.LoadBodyAsync(info.ResponseBodyPath, info.ResponseBody);
        res.Content.Text = body;
        res.Content.Size = body?.Length ?? 0;
        res.Content.MimeType = res.Headers.FirstOrDefault(h => h.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))?.Value ?? "";
        
        res.BodySize = res.Content.Size;
        res.HeadersSize = -1;

        return res;
    }
}
