using System;

namespace ProxyGuy.WinForms
{
    public class RequestInfo
    {
        public DateTime Time { get; set; } = DateTime.Now;
        public string Method { get; set; }
        public string Url { get; set; }
        public string Domain { get; set; }
        public int StatusCode { get; set; }
        public System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>> RequestHeaders { get; set; } = new();
        public System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>> ResponseHeaders { get; set; } = new();
        public string RequestBody { get; set; }
        public string ResponseBody { get; set; }
    }
}
