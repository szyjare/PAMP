using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PAMP;

public record ComponentVersionInfo(
    string Version,
    string DisplayName,
    string DownloadUrl,
    string Notes,
    bool IsRecommended = false,
    bool IsCkeCompatible = false,
    string? Sha256 = null,
    long SizeBytes = 0
);

public class ComponentPackage
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string License { get; init; }
    public required string LicenseUrl { get; init; }
    public required string TargetSubfolder { get; init; }
    public required IReadOnlyList<ComponentVersionInfo> Versions { get; set; }
}

public class VersionComparer : IComparer<string?>
{
    public static readonly VersionComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        string cleanX = x.Trim().TrimStart('v', 'V');
        string cleanY = y.Trim().TrimStart('v', 'V');

        var partsX = cleanX.Split(['.', '-', '_', '+'], StringSplitOptions.RemoveEmptyEntries);
        var partsY = cleanY.Split(['.', '-', '_', '+'], StringSplitOptions.RemoveEmptyEntries);

        int maxLen = Math.Max(partsX.Length, partsY.Length);
        for (int i = 0; i < maxLen; i++)
        {
            string px = i < partsX.Length ? partsX[i] : "0";
            string py = i < partsY.Length ? partsY[i] : "0";

            bool isNumX = int.TryParse(px, out int nx);
            bool isNumY = int.TryParse(py, out int ny);

            if (isNumX && isNumY)
            {
                int cmp = nx.CompareTo(ny);
                if (cmp != 0) return cmp;
            }
            else
            {
                int cmp = string.Compare(px, py, StringComparison.OrdinalIgnoreCase);
                if (cmp != 0) return cmp;
            }
        }

        return 0;
    }
}

public static class ComponentCatalog
{
    public const string DefaultRepositoryManifestUrl = "https://repo.sjarecki.pl/api/pamp/manifest";
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(7) };

    private static readonly List<ComponentPackage> DefaultPackages =
    [
        new ComponentPackage
        {
            Id = "php",
            Name = "PHP (Hypertext Preprocessor)",
            License = "PHP License 3.01",
            LicenseUrl = "https://www.php.net/license/3_01.txt",
            TargetSubfolder = "php",
            Versions =
            [
                new("8.5.10", "PHP 8.5.10 (VS17 x64 TS)", "https://repo.sjarecki.pl/api/files/packages/ioww0esuzio9z5c/php_8_5_Ac7pRNlkpW.0-Win32-vs17-x64.zip", "Wydanie deweloperskie"),
                new("8.2.12", "PHP 8.2.12 (VS16 x64 TS) - CKE", "https://repo.sjarecki.pl/api/files/packages/f3lm07mbl0uku1y/php_8_2_JbdsQEeEoh.2-Win32-vs16-x64.zip", "Zgodne z CKE", IsRecommended: true, IsCkeCompatible: true)
            ]
        },
        new ComponentPackage
        {
            Id = "apache",
            Name = "Apache HTTP Server",
            License = "Apache License 2.0",
            LicenseUrl = "https://www.apache.org/licenses/LICENSE-2.0",
            TargetSubfolder = "apache",
            Versions =
            [
                new("2.4.68", "Apache 2.4.68 Win64", "https://repo.sjarecki.pl/api/files/packages/gpxca54e9t3lj4g/httpd_2_4_hFeYTbCVSB.60827-Win64-VS18.zip", "Stabilne wydanie Apache 2.4", IsRecommended: true),
                new("2.4.58", "Apache 2.4.58 Win64 - CKE", "https://repo.sjarecki.pl/api/files/packages/e2be2s784cu1lzj/apache_2_4_58_winx64_2PbNelJKwl.zip.zip", "Zgodne z CKE", IsCkeCompatible: true)
            ]
        },
        new ComponentPackage
        {
            Id = "mariadb",
            Name = "MariaDB Database Server",
            License = "GNU General Public License v2.0",
            LicenseUrl = "https://mariadb.com/kb/en/mariadb-license/",
            TargetSubfolder = "mariadb",
            Versions =
            [
                new("12.3.3", "MariaDB 12.3.3", "https://repo.sjarecki.pl/api/files/packages/134c5r21r2i6odx/mariadb_12_3_IEC41MIVLV.3-winx64.zip", "Najnowsze wydanie"),
                new("10.4.32", "MariaDB 10.4.32 - CKE", "https://repo.sjarecki.pl/api/files/packages/ofpkqom94g7tyoh/mariadb_10_4_n8yRHEBNqJ.32-winx64.zip", "Zgodne z CKE", IsRecommended: true, IsCkeCompatible: true)
            ]
        },
        new ComponentPackage
        {
            Id = "phpmyadmin",
            Name = "phpMyAdmin",
            License = "GNU General Public License v2.0",
            LicenseUrl = "https://www.phpmyadmin.net/license/",
            TargetSubfolder = "phpmyadmin",
            Versions =
            [
                new("5.2.3", "phpMyAdmin 5.2.3", "https://repo.sjarecki.pl/api/files/packages/9kdfn86ui1tukpo/php_my_admin_5_2_bt7MGmqNxO.3-all-languages.zip", "Najnowsze wydanie"),
                new("5.2.1", "phpMyAdmin 5.2.1 - CKE", "https://repo.sjarecki.pl/api/files/packages/4b7i3vq6o9qsdqz/php_my_admin_5_2_v0OhQZRMWk.1-all-languages.zip", "Zgodne z CKE", IsRecommended: true, IsCkeCompatible: true)
            ]
        }
    ];

    private static IReadOnlyList<ComponentPackage> _packages = DefaultPackages;
    private static Task<bool>? _initializationTask;
    private static readonly object _lock = new();

    public static IReadOnlyList<ComponentPackage> Packages => _packages;

    public static Task<bool> EnsureLoadedAsync()
    {
        lock (_lock)
        {
            _initializationTask ??= LoadFromRemoteAsync();
            return _initializationTask;
        }
    }

    public static async Task<bool> LoadFromRemoteAsync(string? manifestUrl = null)
    {
        string url = manifestUrl ?? DefaultRepositoryManifestUrl;
        string cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PAMP", "cached_manifest.json");

        try
        {
            using var response = await HttpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var manifest = JsonSerializer.Deserialize<RemoteManifestDto>(json);
                if (manifest?.Components != null && manifest.Components.Count > 0)
                {
                    ApplyRemoteManifest(manifest);
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
                        await File.WriteAllTextAsync(cachePath, json);
                    }
                    catch { }
                    return true;
                }
            }
        }
        catch
        {
            // Network error - try local cache
        }

        // Try to load cached manifest
        try
        {
            if (File.Exists(cachePath))
            {
                string cachedJson = await File.ReadAllTextAsync(cachePath);
                var cachedManifest = JsonSerializer.Deserialize<RemoteManifestDto>(cachedJson);
                if (cachedManifest?.Components != null && cachedManifest.Components.Count > 0)
                {
                    ApplyRemoteManifest(cachedManifest);
                    return true;
                }
            }
        }
        catch { }

        // Ensure default packages are also sorted descending
        SortPackageVersions(DefaultPackages);
        _packages = DefaultPackages;
        return false;
    }

    private static void ApplyRemoteManifest(RemoteManifestDto manifest)
    {
        var result = new List<ComponentPackage>();

        var templatePackages = new Dictionary<string, (string Name, string License, string LicenseUrl, string Subfolder)>(StringComparer.OrdinalIgnoreCase)
        {
            ["php"] = ("PHP (Hypertext Preprocessor)", "PHP License 3.01", "https://www.php.net/license/3_01.txt", "php"),
            ["apache"] = ("Apache HTTP Server", "Apache License 2.0", "https://www.apache.org/licenses/LICENSE-2.0", "apache"),
            ["mariadb"] = ("MariaDB Database Server", "GNU General Public License v2.0", "https://mariadb.com/kb/en/mariadb-license/", "mariadb"),
            ["phpmyadmin"] = ("phpMyAdmin", "GNU General Public License v2.0", "https://www.phpmyadmin.net/license/", "phpmyadmin")
        };

        foreach (var (pkgId, meta) in templatePackages)
        {
            var versionsList = new List<ComponentVersionInfo>();

            if (manifest.Components != null && manifest.Components.TryGetValue(pkgId, out var remoteItems))
            {
                foreach (var item in remoteItems)
                {
                    string display = item.DisplayName;
                    if (item.IsCke && !display.Contains("CKE", StringComparison.OrdinalIgnoreCase))
                    {
                        display = $"{display} - CKE";
                    }

                    versionsList.Add(new ComponentVersionInfo(
                        Version: item.Version,
                        DisplayName: display,
                        DownloadUrl: item.Url,
                        Notes: item.Notes ?? (item.IsCke ? "Zgodne ze standardem CKE" : ""),
                        IsRecommended: item.IsCke,
                        IsCkeCompatible: item.IsCke,
                        Sha256: item.Sha256,
                        SizeBytes: item.SizeBytes
                    ));
                }
            }

            // Fallback to default versions if remote had 0 versions for this component
            if (versionsList.Count == 0)
            {
                var defPkg = DefaultPackages.FirstOrDefault(p => p.Id == pkgId);
                if (defPkg != null) versionsList.AddRange(defPkg.Versions);
            }

            // Najnowsza wersja zawsze na górze (indeks 0)
            var sortedVersions = versionsList
                .OrderByDescending(v => v.Version, VersionComparer.Instance)
                .ToList();

            result.Add(new ComponentPackage
            {
                Id = pkgId,
                Name = meta.Name,
                License = meta.License,
                LicenseUrl = meta.LicenseUrl,
                TargetSubfolder = meta.Subfolder,
                Versions = sortedVersions
            });
        }

        _packages = result;
    }

    private static void SortPackageVersions(List<ComponentPackage> pkgs)
    {
        foreach (var p in pkgs)
        {
            p.Versions = p.Versions
                .OrderByDescending(v => v.Version, VersionComparer.Instance)
                .ToList();
        }
    }

    public static IReadOnlyList<(ComponentPackage Package, ComponentVersionInfo Version)> GetCkeProfile()
    {
        var list = new List<(ComponentPackage, ComponentVersionInfo)>();

        foreach (var pkg in Packages)
        {
            // Pobierz wersję oznaczoną jako CKE; jeśli jej brak, pobierz najnowszą dostępną wersję
            var ckeVer = pkg.Versions.FirstOrDefault(v => v.IsCkeCompatible) ?? pkg.Versions.FirstOrDefault();
            if (ckeVer != null)
            {
                list.Add((pkg, ckeVer));
            }
        }

        return list;
    }
}

public class RemoteManifestDto
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; }

    [JsonPropertyName("generatedAt")]
    public string? GeneratedAt { get; set; }

    [JsonPropertyName("ckeProfile")]
    public Dictionary<string, string?>? CkeProfile { get; set; }

    [JsonPropertyName("components")]
    public Dictionary<string, List<RemoteComponentItemDto>>? Components { get; set; }
}

public class RemoteComponentItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }

    [JsonPropertyName("isCke")]
    public bool IsCke { get; set; }

    [JsonPropertyName("sizeBytes")]
    public long SizeBytes { get; set; }

    [JsonPropertyName("releaseDate")]
    public string? ReleaseDate { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

