using System;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Linq;

namespace ProxyGuy.WinForms
{
    public static class WindowsProxyHelper
    {
        private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        private const int INTERNET_OPTION_REFRESH = 37;

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

        private static string? _previousOverride;
        private static bool _modifiedOverride;

        public static void Enable(string host, int port)
        {
            using var registry = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", writable: true);
            if (registry == null) return;

            _modifiedOverride = false;
            _previousOverride = registry.GetValue("ProxyOverride") as string;
            if (!string.IsNullOrWhiteSpace(_previousOverride))
            {
                var parts = _previousOverride.Split(';');
                var filtered = string.Join(";",
                    parts.Where(p => !string.Equals(p.Trim(), "<local>", StringComparison.OrdinalIgnoreCase)
                                     && !string.IsNullOrWhiteSpace(p)));
                if (!string.Equals(_previousOverride, filtered, StringComparison.Ordinal))
                {
                    registry.SetValue("ProxyOverride", filtered);
                    _modifiedOverride = true;
                }
            }

            registry.SetValue("ProxyServer", $"{host}:{port}");
            registry.SetValue("ProxyEnable", 1);
            Environment.SetEnvironmentVariable("HTTP_PROXY", $"http://{host}:{port}", EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", $"http://{host}:{port}", EnvironmentVariableTarget.Process);
            Refresh();
        }

        public static void Disable()
        {
            using var registry = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", writable: true);
            if (registry == null) return;
            registry.SetValue("ProxyEnable", 0);
            registry.DeleteValue("ProxyServer", false);
            Environment.SetEnvironmentVariable("HTTP_PROXY", null, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", null, EnvironmentVariableTarget.Process);

            if (_modifiedOverride)
            {
                if (_previousOverride != null)
                {
                    registry.SetValue("ProxyOverride", _previousOverride);
                }
                else
                {
                    registry.DeleteValue("ProxyOverride", false);
                }
            }

            Refresh();
        }

        private static void Refresh()
        {
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }
    }
}