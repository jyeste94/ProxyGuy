using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ProxyGuy.WinForms
{
    public static class PhpIniHelper
    {
        public static void ConfigurePhpCertificate(string pemPath)
        {
            ConfigurePhpCertificate(pemPath, GetPhpIniFiles());
        }

        public static void ConfigurePhpCertificate(string pemPath, IEnumerable<string> iniFiles)
        {
            foreach (var ini in iniFiles)
            {
                try
                {
                    UpdateIniFile(ini, pemPath);
                }
                catch (Exception)
                {
                    // Ignorar errores individuales
                }
            }
        }

        public static void RestorePhpConfiguration()
        {
            RestorePhpConfiguration(GetPhpIniFiles());
        }

        public static void RestorePhpConfiguration(IEnumerable<string> iniFiles)
        {
            foreach (var ini in iniFiles)
            {
                var backup = ini + ".proxyguy.bak";
                try
                {
                    if (File.Exists(backup))
                    {
                        File.Copy(backup, ini, true);
                        File.Delete(backup);
                    }
                }
                catch (Exception)
                {
                    // Ignorar errores individuales
                }
            }
        }

        private static IEnumerable<string> GetPhpIniFiles()
        {
            var files = new List<string>();
            try
            {
                var psi = new ProcessStartInfo("php", "--ini")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    var output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(2000);
                    var loaded = Regex.Match(output, @"Loaded Configuration File:\s*(.+)");
                    if (loaded.Success && File.Exists(loaded.Groups[1].Value.Trim()))
                        files.Add(loaded.Groups[1].Value.Trim());
                    var scan = Regex.Match(output, @"Additional .ini files parsed:\s*(.+)");
                    if (scan.Success)
                    {
                        foreach (var f in scan.Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        {
                            var path = f.Trim();
                            if (File.Exists(path)) files.Add(path);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // php no encontrado o error al ejecutar
            }
            return files.Distinct();
        }

        private static void UpdateIniFile(string iniPath, string pemPath)
        {
            var backup = iniPath + ".proxyguy.bak";
            if (!File.Exists(backup))
            {
                File.Copy(iniPath, backup, true);
            }

            var lines = File.ReadAllLines(iniPath).ToList();
            bool curlSet = false;
            bool opensslSet = false;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].TrimStart();
                if (line.StartsWith("curl.cainfo", StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = $"curl.cainfo=\"{pemPath}\"";
                    curlSet = true;
                }
                else if (line.StartsWith("openssl.cafile", StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = $"openssl.cafile=\"{pemPath}\"";
                    opensslSet = true;
                }
            }
            if (!curlSet)
            {
                lines.Add($"curl.cainfo=\"{pemPath}\"");
            }
            if (!opensslSet)
            {
                lines.Add($"openssl.cafile=\"{pemPath}\"");
            }
            File.WriteAllLines(iniPath, lines);
        }
    }
}
