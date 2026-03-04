using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProxyGuy.Models.Har;

// HAR 1.2 Spec Models

public class HarRoot
{
    [JsonPropertyName("log")]
    public HarLog Log { get; set; } = new();
}

public class HarLog
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.2";

    [JsonPropertyName("creator")]
    public HarCreator Creator { get; set; } = new();

    [JsonPropertyName("entries")]
    public List<HarEntry> Entries { get; set; } = new();
}

public class HarCreator
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "ProxyGuy";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";
}

public class HarEntry
{
    [JsonPropertyName("startedDateTime")]
    public string StartedDateTime { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public double Time { get; set; }

    [JsonPropertyName("request")]
    public HarRequest Request { get; set; } = new();

    [JsonPropertyName("response")]
    public HarResponse Response { get; set; } = new();

    [JsonPropertyName("cache")]
    public HarCache Cache { get; set; } = new();

    [JsonPropertyName("timings")]
    public HarTimings Timings { get; set; } = new();
}

public class HarRequest
{
    [JsonPropertyName("method")]
    public string Method { get; set; } = "GET";

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("httpVersion")]
    public string HttpVersion { get; set; } = "HTTP/1.1";

    [JsonPropertyName("cookies")]
    public List<HarCookie> Cookies { get; set; } = new();

    [JsonPropertyName("headers")]
    public List<HarHeader> Headers { get; set; } = new();

    [JsonPropertyName("queryString")]
    public List<HarQueryParam> QueryString { get; set; } = new();

    [JsonPropertyName("postData")]
    public HarPostData? PostData { get; set; }

    [JsonPropertyName("headersSize")]
    public int HeadersSize { get; set; } = -1;

    [JsonPropertyName("bodySize")]
    public int BodySize { get; set; } = -1;
}

public class HarResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("statusText")]
    public string StatusText { get; set; } = string.Empty;

    [JsonPropertyName("httpVersion")]
    public string HttpVersion { get; set; } = "HTTP/1.1";

    [JsonPropertyName("cookies")]
    public List<HarCookie> Cookies { get; set; } = new();

    [JsonPropertyName("headers")]
    public List<HarHeader> Headers { get; set; } = new();

    [JsonPropertyName("content")]
    public HarContent Content { get; set; } = new();

    [JsonPropertyName("redirectURL")]
    public string RedirectURL { get; set; } = string.Empty;

    [JsonPropertyName("headersSize")]
    public int HeadersSize { get; set; } = -1;

    [JsonPropertyName("bodySize")]
    public int BodySize { get; set; } = -1;
}

public class HarCookie
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class HarHeader
{
     [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class HarQueryParam
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class HarPostData
{
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class HarContent
{
    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class HarCache
{
}

public class HarTimings
{
    [JsonPropertyName("send")]
    public double Send { get; set; } = 0;

    [JsonPropertyName("wait")]
    public double Wait { get; set; } = 0;

    [JsonPropertyName("receive")]
    public double Receive { get; set; } = 0;
}
