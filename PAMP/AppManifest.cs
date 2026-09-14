using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PAMP;

public class AppManifest
{
    [JsonPropertyName("versions")]
    public ComponentVersions Versions { get; set; } = new();
}

public class ComponentVersions
{
    [JsonPropertyName("pamp")]
    public string Pamp { get; set; } = "1.2.1";

    [JsonPropertyName("php")]
    public string Php { get; set; } = "—";

    [JsonPropertyName("apache")]
    public string Apache { get; set; } = "—";

    [JsonPropertyName("mariadb")]
    public string MariaDb { get; set; } = "—";

    [JsonPropertyName("phpmyadmin")]
    public string PhpMyAdmin { get; set; } = "—";
}

public static class ManifestLoader
{
    private static readonly string UserManifestPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PAMP", "installed_versions.json");

    private static readonly string LegacyAppManifestPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "manifest.json");

    public static AppManifest Load()
    {
        // 1. Sprawdź plik konfiguracji w AppData użytkownika
        if (File.Exists(UserManifestPath))
        {
            try
            {
                string json = File.ReadAllText(UserManifestPath);
                var manifest = JsonSerializer.Deserialize<AppManifest>(json);
                if (manifest?.Versions != null)
                {
                    manifest.Versions.Pamp = GetAppVersion();
                    FillMissingFromDetection(manifest.Versions);
                    return manifest;
                }
            }
            catch { }
        }

        // 2. Sprawdź ewentualny manifest w katalogu aplikacji
        if (File.Exists(LegacyAppManifestPath))
        {
            try
            {
                string json = File.ReadAllText(LegacyAppManifestPath);
                var manifest = JsonSerializer.Deserialize<AppManifest>(json);
                if (manifest?.Versions != null)
                {
                    manifest.Versions.Pamp = GetAppVersion();
                    FillMissingFromDetection(manifest.Versions);
                    return manifest;
                }
            }
            catch { }
        }

        // 3. Fallback: wykryj wersje z obecnych plików binarnych
        var fallback = new AppManifest();
        fallback.Versions.Pamp = GetAppVersion();
        TryDetectBinaries(fallback.Versions);
        return fallback;
    }

    public static void Save(AppManifest manifest)
    {
        try
        {
            manifest.Versions.Pamp = GetAppVersion();
            string json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });

            Directory.CreateDirectory(Path.GetDirectoryName(UserManifestPath)!);
            File.WriteAllText(UserManifestPath, json);

            try
            {
                File.WriteAllText(LegacyAppManifestPath, json);
            }
            catch { }
        }
        catch { }
    }

    public static string GetAppVersion()
    {
        var ver = typeof(AppManifest).Assembly.GetName().Version;
        return ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "1.2.1";
    }

    private static void FillMissingFromDetection(ComponentVersions v)
    {
        var detected = new ComponentVersions();
        TryDetectBinaries(detected);

        if (v.Php == "—" || string.IsNullOrWhiteSpace(v.Php) || v.Php == "?.?.?") v.Php = detected.Php;
        if (v.Apache == "—" || string.IsNullOrWhiteSpace(v.Apache) || v.Apache == "?.?.?") v.Apache = detected.Apache;
        if (v.MariaDb == "—" || string.IsNullOrWhiteSpace(v.MariaDb) || v.MariaDb == "?.?.?") v.MariaDb = detected.MariaDb;
        if (v.PhpMyAdmin == "—" || string.IsNullOrWhiteSpace(v.PhpMyAdmin) || v.PhpMyAdmin == "?.?.?") v.PhpMyAdmin = detected.PhpMyAdmin;
    }

    private static void TryDetectBinaries(ComponentVersions v)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // PHP
        string phpExe = Path.Combine(baseDir, "bin", "php", "php.exe");
        if (File.Exists(phpExe))
        {
            try
            {
                var fvi = FileVersionInfo.GetVersionInfo(phpExe);
                if (!string.IsNullOrWhiteSpace(fvi.ProductVersion))
                    v.Php = fvi.ProductVersion.Split('+')[0].Trim();
            }
            catch { }
        }

        // Apache
        string httpdExe = Path.Combine(baseDir, "bin", "apache", "bin", "httpd.exe");
        if (File.Exists(httpdExe))
        {
            try
            {
                var fvi = FileVersionInfo.GetVersionInfo(httpdExe);
                if (!string.IsNullOrWhiteSpace(fvi.ProductVersion))
                    v.Apache = fvi.ProductVersion.Split('+')[0].Trim();
            }
            catch { }
        }

        // MariaDB
        string mariaExe = Path.Combine(baseDir, "bin", "mariadb", "bin", "mariadbd.exe");
        if (!File.Exists(mariaExe)) mariaExe = Path.Combine(baseDir, "bin", "mariadb", "bin", "mysqld.exe");
        if (File.Exists(mariaExe))
        {
            try
            {
                var fvi = FileVersionInfo.GetVersionInfo(mariaExe);
                if (!string.IsNullOrWhiteSpace(fvi.ProductVersion))
                    v.MariaDb = fvi.ProductVersion.Split('+')[0].Trim();
            }
            catch { }
        }

        // phpMyAdmin
        string pmaVersionFile = Path.Combine(baseDir, "bin", "phpmyadmin", "RELEASE_DATE");
        if (File.Exists(pmaVersionFile))
        {
            // Możliwa wersja w nazwach folderów lub package.json
            string packageJson = Path.Combine(baseDir, "bin", "phpmyadmin", "package.json");
            if (File.Exists(packageJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(packageJson));
                    if (doc.RootElement.TryGetProperty("version", out var verProp))
                    {
                        v.PhpMyAdmin = verProp.GetString() ?? v.PhpMyAdmin;
                    }
                }
                catch { }
            }
        }
    }
}