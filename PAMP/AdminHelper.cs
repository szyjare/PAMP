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
            Application.Current.Shutdown();
            return true;
        }
        catch
        {
            return false; // Użytkownik odrzucił monit UAC
        }
    }
}
