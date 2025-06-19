using System;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace ProxyGuy.WinForms
{
    public static class WindowsProxyHelper
    {
        private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        private const int INTERNET_OPTION_REFRESH = 37;

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

        public static void Enable(string host, int port)
        {
            using var registry = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", writable: true);
            if (registry == null) return;
            registry.SetValue("ProxyServer", $"{host}:{port}");
            registry.SetValue("ProxyEnable", 1);
            Refresh();
        }

        public static void Disable()
        {
            using var registry = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", writable: true);
            if (registry == null) return;
            registry.SetValue("ProxyEnable", 0);
            registry.DeleteValue("ProxyServer", false);
            Refresh();
        }

        private static void Refresh()
        {
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }
    }
}
