using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Microsoft.Maui.Storage;

namespace ProxyGuy.Services;

public partial class PhpIntegrationManager : ObservableObject
{
    private static readonly PhpIntegrationManager _instance = new();
    public static PhpIntegrationManager Instance => _instance;
    
    public ObservableCollection<string> PhpIniPaths { get; } = new();

    private string PemPath => Path.Combine(FileSystem.AppDataDirectory, "rootCert.pem");

    private PhpIntegrationManager() { }

    public void StartIntegration()
    {
        if (!File.Exists(PemPath)) return;

        foreach (var path in PhpIniPaths)
        {
            if (File.Exists(path))
            {
                BackupAndPatch(path);
            }
        }
    }

    public void StopIntegration()
    {
        foreach (var path in PhpIniPaths)
        {
            RestoreBackup(path);
        }
    }

    private void BackupAndPatch(string path)
    {
        try
        {
            var backupPath = path + ".proxyguy.bak";
            
            // If backup exists, we might have crashed before restoring. 
            // Assume backup is the clean original.
            if (!File.Exists(backupPath))
            {
                File.Copy(path, backupPath);
            }
            
            // Read content from BACKUP to ensure we always patch source
            var content = File.ReadAllText(backupPath);
            
            // Patch logic
            var pemPathEscaped = PemPath.Replace("\\", "/"); // Ensure forward slashes for PHP config compatibility
            
            // Remove existing settings if present (to avoid duplicates or conflicts)
            // Actually, we are reading from clean backup, so we just append or replace.
            
            // We'll just start fresh from clean content and append our settings at the end
            // Or replace if we want to be fancy. Appending is safer usually for overrides.
            // But if [curl] section exists, might be better to be near it.
            // Simple approach: Append to end.
            
            var patchedContent = content + $"\r\n\r\n; --- ProxyGuy Auto-Configuration ---\r\ncurl.cainfo=\"{pemPathEscaped}\"\r\nopenssl.cafile=\"{pemPathEscaped}\"\r\n; ----------------------------\r\n";
            
            File.WriteAllText(path, patchedContent);
            System.Diagnostics.Debug.WriteLine($"[PHP] Patched {path}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PHP] Failed to patch {path}: {ex.Message}");
        }
    }

    private void RestoreBackup(string path)
    {
        try
        {
            var backupPath = path + ".proxyguy.bak";
            if (File.Exists(backupPath))
            {
                // Restore
                File.Copy(backupPath, path, true); // Overwrite patched version
                File.Delete(backupPath); // Delete backup
                System.Diagnostics.Debug.WriteLine($"[PHP] Restored {path}");
            }
        }
        catch (Exception ex)
        {
             System.Diagnostics.Debug.WriteLine($"[PHP] Failed to restore {path}: {ex.Message}");
        }
    }
}
