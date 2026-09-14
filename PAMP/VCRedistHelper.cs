using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace PAMP;

public static class VCRedistHelper
{
    public const string VcRedistDownloadUrl = "https://aka.ms/vs/17/release/vc_redist.x64.exe";

    public static bool IsInstalled()
    {
        try
        {
            // 1. Sprawdz obecnosc kluczowego pliku vcruntime140.dll w C:\Windows\System32
            string vcRuntimePath = Path.Combine(Environment.SystemDirectory, "vcruntime140.dll");
            if (!File.Exists(vcRuntimePath))
            {
                return false;
            }

            // 2. Sprawdz oficjalny wpis w rejestrze Windows (Visual C++ 2015-2022 v14.x x64)
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var subKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64");
            if (subKey != null)
            {
                var installed = subKey.GetValue("Installed");
                if (installed is int i && i == 1)
                {
                    return true;
                }
            }

            // Fallback: obecnosc pliku w katalogu System32
            return File.Exists(vcRuntimePath);
        }
        catch
        {
            return File.Exists(Path.Combine(Environment.SystemDirectory, "vcruntime140.dll"));
        }
    }

    public static bool CheckAndPrompt(Window? owner = null)
    {
        if (IsInstalled()) return true;

        string title = TranslationSource.Instance["vcRedistMissingTitle"] ?? "Wymagany pakiet Visual C++";
        string message = TranslationSource.Instance["vcRedistMissingMessage"] ??
            "W systemie nie znaleziono pakietu Microsoft Visual C++ 2015–2022 Redistributable (x64).\n\n" +
            "Jest on niezbędny do poprawnego działania modułów Apache, PHP oraz MariaDB.\n\n" +
            "Czy chcesz teraz pobrać oficjalny instalator ze strony Microsoftu?";

        var result = owner != null
            ? MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning)
            : MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                Process.Start(new ProcessStartInfo(VcRedistDownloadUrl) { UseShellExecute = true });
            }
            catch { }
        }

        return false;
    }
}
