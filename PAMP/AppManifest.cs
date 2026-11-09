using System.Reflection;
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
    public string Pamp { get; set; } = "1.4.0";

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
    // Wspólny manifest w katalogu bin (dostępny dla wszystkich użytkowników)
    private static readonly string SharedBinManifestPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "bin", "installed_versions.json");

    // Wspólny manifest w ProgramData (dla całego komputera)
    private static readonly string CommonDataManifestPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PAMP", "installed_versions.json");

    // Przestarzały manifest w AppData bieżącego użytkownika (usuwany, aby zapobiec rozbieżnościom między kontami)
    private static readonly string StaleUserManifestPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PAMP", "installed_versions.json");

    // Manifest w głównym katalogu aplikacji (wersja portable)
    private static readonly string LegacyAppManifestPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "manifest.json");

    public static bool AreEssentialComponentsInstalled()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        bool php = File.Exists(Path.Combine(baseDir, "bin", "php", "php.exe"));
        bool apache = File.Exists(Path.Combine(baseDir, "bin", "apache", "bin", "httpd.exe"));
        bool maria = File.Exists(Path.Combine(baseDir, "bin", "mariadb", "bin", "mariadbd.exe")) ||
                     File.Exists(Path.Combine(baseDir, "bin", "mariadb", "bin", "mysqld.exe"));

        // Weryfikacja fizycznej obecności plików na dysku
        return php && apache && maria;
    }

    public static bool HasAnyComponentInstalled()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        bool php = File.Exists(Path.Combine(baseDir, "bin", "php", "php.exe"));
        bool apache = File.Exists(Path.Combine(baseDir, "bin", "apache", "bin", "httpd.exe"));
        bool maria = File.Exists(Path.Combine(baseDir, "bin", "mariadb", "bin", "mariadbd.exe")) ||
                     File.Exists(Path.Combine(baseDir, "bin", "mariadb", "bin", "mysqld.exe"));

        return php || apache || maria;
    }

    public static AppManifest Load()
    {
        // 1. Usuń przestarzały manifest z AppData bieżącego użytkownika jeśli istnieje
        if (!AppPaths.IsPortable)
        {
            try
            {
                if (File.Exists(StaleUserManifestPath))
                {
                    File.Delete(StaleUserManifestPath);
                }
            }
            catch { }
        }

        // 2. Szukamy wyłącznie w lokalizacjach współdzielonych
        string[] candidates = [SharedBinManifestPath, CommonDataManifestPath, LegacyAppManifestPath];
        AppManifest? loaded = null;

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var manifest = JsonSerializer.Deserialize<AppManifest>(json);
                    if (manifest?.Versions != null)
                    {
                        loaded = manifest;
                        break;
                    }
                }
                catch { }
            }
        }

        var result = loaded ?? new AppManifest();
        result.Versions ??= new ComponentVersions();
        result.Versions.Pamp = GetAppVersion();

        // 3. KLUCZOWE: Zawsze synchronizujemy z fizycznym stanem dysku!
        // Jeśli pliki nie istnieją na dysku, wersja MUSI być "—"
        ValidateAndSyncWithDisk(result.Versions);

        return result;
    }

    public static void Save(AppManifest manifest)
    {
        try
        {
            manifest.Versions.Pamp = GetAppVersion();
            ValidateAndSyncWithDisk(manifest.Versions);

            string json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });

            // 1. Zapisz w katalogu bin (wspólnym dla wszystkich użytkowników)
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SharedBinManifestPath)!);
                File.WriteAllText(SharedBinManifestPath, json);
            }
            catch { }

            // 2. Zapisz w ProgramData (wspólnym dla całego komputera) - pomijane w trybie portable
            if (!AppPaths.IsPortable)
            {
                try
                {
                    string progDataDir = Path.GetDirectoryName(CommonDataManifestPath)!;
                    Directory.CreateDirectory(progDataDir);
                    File.WriteAllText(CommonDataManifestPath, json);

                    if (AdminHelper.IsAdministrator)
                    {
                        ComponentInstallerService.EnsureDirectoryPermissions(progDataDir);
                    }
                }
                catch { }
            }

            // 3. Zapisz w katalogu aplikacji (dla wersji portable)
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
        var infoVer = typeof(AppManifest).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(infoVer))
        {
            return infoVer.Split('+')[0].Trim();
        }

        var ver = typeof(AppManifest).Assembly.GetName().Version;
        return ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "1.4.0";
    }

    private static void ValidateAndSyncWithDisk(ComponentVersions v)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var detected = new ComponentVersions();
        TryDetectBinaries(detected);

        // PHP
        string phpExe = Path.Combine(baseDir, "bin", "php", "php.exe");
        if (!File.Exists(phpExe))
        {
            v.Php = "—";
        }
        else if (v.Php == "—" || string.IsNullOrWhiteSpace(v.Php) || v.Php == "?.?.?")
        {
            v.Php = detected.Php;
        }

        // Apache
        string httpdExe = Path.Combine(baseDir, "bin", "apache", "bin", "httpd.exe");
        if (!File.Exists(httpdExe))
        {
            v.Apache = "—";
        }
        else if (v.Apache == "—" || string.IsNullOrWhiteSpace(v.Apache) || v.Apache == "?.?.?")
        {
            v.Apache = detected.Apache;
        }

        // MariaDB
        string mariaExe = Path.Combine(baseDir, "bin", "mariadb", "bin", "mariadbd.exe");
        if (!File.Exists(mariaExe)) mariaExe = Path.Combine(baseDir, "bin", "mariadb", "bin", "mysqld.exe");
        if (!File.Exists(mariaExe))
        {
            v.MariaDb = "—";
        }
        else if (v.MariaDb == "—" || string.IsNullOrWhiteSpace(v.MariaDb) || v.MariaDb == "?.?.?")
        {
            v.MariaDb = detected.MariaDb;
        }

        // phpMyAdmin
        string pmaDir = Path.Combine(baseDir, "bin", "phpmyadmin");
        if (!Directory.Exists(pmaDir) || !File.Exists(Path.Combine(pmaDir, "index.php")))
        {
            v.PhpMyAdmin = "—";
        }
        else if (v.PhpMyAdmin == "—" || string.IsNullOrWhiteSpace(v.PhpMyAdmin) || v.PhpMyAdmin == "?.?.?")
        {
            v.PhpMyAdmin = detected.PhpMyAdmin;
        }
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
        string pmaDir = Path.Combine(baseDir, "bin", "phpmyadmin");
        if (Directory.Exists(pmaDir))
        {
            // 1. Sprawdź package.json
            string packageJson = Path.Combine(pmaDir, "package.json");
            if (File.Exists(packageJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(packageJson));
                    if (doc.RootElement.TryGetProperty("version", out var verProp))
                    {
                        string? ver = verProp.GetString();
                        if (!string.IsNullOrWhiteSpace(ver))
                        {
                            v.PhpMyAdmin = ver.Trim();
                        }
                    }
                }
                catch { }
            }

            // 2. Jeśli nie odczytano z package.json, sprawdź plik RELEASE-DATE-*
            if (v.PhpMyAdmin == "—" || string.IsNullOrWhiteSpace(v.PhpMyAdmin))
            {
                try
                {
                    var relFile = Directory.EnumerateFiles(pmaDir, "RELEASE-DATE-*").FirstOrDefault()
                                  ?? Directory.EnumerateFiles(pmaDir, "RELEASE_DATE*").FirstOrDefault();
                    if (relFile != null)
                    {
                        string fn = Path.GetFileName(relFile);
                        string verFromFn = fn.Replace("RELEASE-DATE-", "", StringComparison.OrdinalIgnoreCase)
                                             .Replace("RELEASE_DATE-", "", StringComparison.OrdinalIgnoreCase)
                                             .Replace("RELEASE_DATE", "", StringComparison.OrdinalIgnoreCase)
                                             .Trim();
                        if (!string.IsNullOrWhiteSpace(verFromFn))
                        {
                            v.PhpMyAdmin = verFromFn;
                        }
                    }
                }
                catch { }
            }

            // 3. Fallback: sprawdź Version.php
            if (v.PhpMyAdmin == "—" || string.IsNullOrWhiteSpace(v.PhpMyAdmin))
            {
                string versionPhp = Path.Combine(pmaDir, "libraries", "classes", "Version.php");
                if (File.Exists(versionPhp))
                {
                    try
                    {
                        string content = File.ReadAllText(versionPhp);
                        var match = System.Text.RegularExpressions.Regex.Match(content, @"VERSION\s*=\s*'([^']+)'");
                        if (match.Success)
                        {
                            v.PhpMyAdmin = match.Groups[1].Value.Trim();
                        }
                    }
                    catch { }
                }
            }

            // 4. Jeśli nadal nieznana, ale folder istnieje i ma index.php
            if ((v.PhpMyAdmin == "—" || string.IsNullOrWhiteSpace(v.PhpMyAdmin)) && File.Exists(Path.Combine(pmaDir, "index.php")))
            {
                v.PhpMyAdmin = "5.x";
            }
        }
    }
}
