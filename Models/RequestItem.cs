using System;

namespace ProxyGuyMAUI.Models;

public class RequestItem
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? Code { get; set; }
    public DateTime Time { get; set; }
    public string Duration { get; set; } = string.Empty;
    public string RequestLabel { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public string Host
    {
        get
        {
            if (Uri.TryCreate(Url, UriKind.Absolute, out var uri))
            {
                return uri.Host;
            }

            return Url;
        }
    }
}