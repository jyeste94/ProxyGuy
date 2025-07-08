using System;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using System.Windows.Forms;


namespace ProxyGuy.WinForms
{
    public class ProxyService
    {
        private readonly ProxyServer _proxyServer;
        private readonly Action<string> _log;

        public ProxyServer Server => _proxyServer;

        public ProxyService(Action<string> logCallback)
        {
            _proxyServer = new ProxyServer();
            _log = logCallback;
        }

        public async Task StartAsync()
        {
            var explicitEndPoint = new ExplicitProxyEndPoint(System.Net.IPAddress.Any, 9090, true);
            _proxyServer.AddEndPoint(explicitEndPoint);

            _proxyServer.BeforeRequest += OnRequest;
            _proxyServer.BeforeResponse += OnResponse;

            _proxyServer.Start();
        }

        private async Task OnRequest(object sender, SessionEventArgs e)
        {
            _log($"REQ: {e.HttpClient.Request.Method} {e.HttpClient.Request.Url}");
            if (e.HttpClient.Request.HasBody)
            {
                try
                {
                    e.UserData = await e.GetRequestBodyAsString();
                }
                catch (Exception ex)
                {
                    e.UserData = $"<error reading body: {ex.Message}>";
                }
            }
        }

        private async Task OnResponse(object sender, SessionEventArgs e)
        {
            var uri = new Uri(e.HttpClient.Request.Url);
            var info = new RequestInfo
            {
                Method = e.HttpClient.Request.Method,
                Url = e.HttpClient.Request.Url,
                Domain = uri.Host,
                StatusCode = e.HttpClient.Response.StatusCode
            };

            foreach (var header in e.HttpClient.Request.Headers)
            {
                info.RequestHeaders.Add(new System.Collections.Generic.KeyValuePair<string, string>(header.Name, header.Value));
            }

            foreach (var header in e.HttpClient.Response.Headers)
            {
                info.ResponseHeaders.Add(new System.Collections.Generic.KeyValuePair<string, string>(header.Name, header.Value));
            }

            if (e.UserData is string body)
            {
                info.RequestBody = body;
            }

            try
            {
                info.ResponseBody = await e.GetResponseBodyAsString();
            }
            catch (Exception ex)
            {
                info.ResponseBody = $"<error reading body: {ex.Message}>";
            }

            var form = Application.OpenForms[0] as ProxyForm;
            form?.AddRequest(info);

            _log($"RES: {e.HttpClient.Response.StatusCode} {e.HttpClient.Request.Url}");
            return;
        }
    }
}