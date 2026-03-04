using System;
using System.IO;
using System.Linq;
using System.Text;
using ProxyGuy.Models;

namespace ProxyGuy.Helpers;

public static class CurlHelper
{
    /// <summary>
    /// Converts a RequestInfo to a cURL command string.
    /// </summary>
    public static string GenerateCurl(RequestInfo requestInfo)
    {
        if (requestInfo == null)
            return string.Empty;

        var sb = new StringBuilder();
        
        // Start with curl command and URL
        sb.Append($"curl '{EscapeSingleQuotes(requestInfo.Url)}'");

        // Add method if not GET
        if (!string.Equals(requestInfo.Method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append($" \\\n  -X {requestInfo.Method}");
        }

        // Add headers
        foreach (var header in requestInfo.RequestHeaders)
        {
            // Skip certain headers that curl adds automatically or are problematic
            if (ShouldSkipHeader(header.Key))
                continue;

            sb.Append($" \\\n  -H '{EscapeSingleQuotes(header.Key)}: {EscapeSingleQuotes(header.Value)}'");
        }

        // Add body if present
        var bodyContent = requestInfo.RequestBody;
        if (string.IsNullOrWhiteSpace(bodyContent) && !string.IsNullOrWhiteSpace(requestInfo.RequestBodyPath) && File.Exists(requestInfo.RequestBodyPath))
        {
            try
            {
                bodyContent = File.ReadAllText(requestInfo.RequestBodyPath);
            }
            catch
            {
                bodyContent = string.Empty;
            }
        }

        if (!string.IsNullOrWhiteSpace(bodyContent))
        {
            var escapedBody = EscapeSingleQuotes(bodyContent);
            sb.Append($" \\\n  --data-raw '{escapedBody}'");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Determines if a header should be skipped in the cURL output.
    /// </summary>
    private static bool ShouldSkipHeader(string headerName)
    {
        var skipHeaders = new[]
        {
            "Host",           // curl sets this automatically
            "Content-Length", // curl calculates this
            "Connection",     // curl manages this
            "Accept-Encoding" // can cause issues with curl
        };

        return skipHeaders.Any(h => string.Equals(h, headerName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Escapes single quotes for use in shell commands.
    /// </summary>
    private static string EscapeSingleQuotes(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Replace single quote with '\''
        return input.Replace("'", "'\\''");
    }
}
