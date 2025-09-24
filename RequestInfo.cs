using System;
using System.Collections.Generic;

namespace ProxyGuyMAUI;

public class RequestInfo
{
    public DateTime Time { get; set; } = DateTime.Now;
    public long Sequence { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public TimeSpan? Duration { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<KeyValuePair<string, string>> RequestHeaders { get; } = new();
    public List<KeyValuePair<string, string>> ResponseHeaders { get; } = new();
    public string RequestBody { get; set; } = string.Empty;
    public string ResponseBody { get; set; } = string.Empty;
}