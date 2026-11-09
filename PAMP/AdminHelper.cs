using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows;

namespace PAMP;

public static class AdminHelper
{
    private static readonly Lazy<bool> _isAdmin = new(() =>
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    });

    public static bool IsAdministrator => _isAdmin.Value;

    /// <summary>
    /// Zwraca true, jeśli proces działa jako Administrator LUB bieżący użytkownik posiada
    /// uprawnienia zapisu do katalogu aplikacji (np. instalacja per-user w AppData lub wersja portable).
    /// </summary>
    public static bool CanManageComponents
    {
        get
        {
            if (IsAdministrator)
                return true;

            if (AppPaths.IsSystemInstalled)
                return false;

            return HasWriteAccessToAppDirectory();
        }
    }

    /// <summary>
    /// Sprawdza, czy bieżący proces ma uprawnienia zapisu do katalogu bin aplikacji.
    /// </summary>
    public static bool HasWriteAccessToAppDirectory()
    {
        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string binDir = Path.Combine(baseDir, "bin");
            if (!Directory.Exists(binDir))
            {
                Directory.CreateDirectory(binDir);
            }

            string testFile = Path.Combine(binDir, $".write_test_{Guid.NewGuid():N}.tmp");
            using (var fs = new FileStream(testFile, FileMode.Create, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose))
            {
                fs.WriteByte(0);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> RestartAsAdministratorAsync(ServerService? serverService = null, string? additionalArgs = null)
    {
        try
        {
            if (serverService != null)
            {
                await serverService.StopAllAsync();
            }

            string exePath = Process.GetCurrentProcess().MainModule?.FileName
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PAMP.exe");

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas",
                Arguments = additionalArgs ?? ""
            };

            Process.Start(psi);
            ShutdownApplication();
            return true;
        }
        catch
        {
            return false; // Użytkownik odrzucił monit UAC
        }
    }

    public static bool RestartAsAdministrator(string? additionalArgs = null)
    {
        try
        {
            string exePath = Process.GetCurrentProcess().MainModule?.FileName
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PAMP.exe");

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas",
                Arguments = additionalArgs ?? ""
            };

            Process.Start(psi);
            ShutdownApplication();
            return true;
        }
        catch
        {
            return false; // Użytkownik odrzucił monit UAC
        }
    }

    private static void ShutdownApplication()
    {
        if (Application.Current != null)
        {
            if (Application.Current.MainWindow is MainWindow mw)
            {
                mw.Dispatcher.Invoke(mw.SetExplicitExit);
            }
            Application.Current.Shutdown();
        }
    }
}