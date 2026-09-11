namespace PAMP;

public record ComponentVersionInfo(
    string Version,
    string DisplayName,
    string DownloadUrl,
    string Notes,
    bool IsRecommended = false,
    bool IsCkeCompatible = false
);

public class ComponentPackage
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string License { get; init; }
    public required string LicenseUrl { get; init; }
    public required string TargetSubfolder { get; init; }
    public required IReadOnlyList<ComponentVersionInfo> Versions { get; init; }
}

public static class ComponentCatalog
{
    public static IReadOnlyList<ComponentPackage> Packages { get; } =
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
                new("8.2.12", "PHP 8.2.12 (VS16 x64 TS) - CKE", "https://windows.php.net/downloads/releases/archives/php-8.2.12-Win32-vs16-x64.zip", "Wymagane przez CKE", IsRecommended: true, IsCkeCompatible: true),
                new("8.3.14", "PHP 8.3.14 (VS16 x64 TS)", "https://windows.php.net/downloads/releases/php-8.3.14-Win32-vs16-x64.zip", "Zalecane dla nowszych projektów"),
                new("8.4.1", "PHP 8.4.1 (VS16 x64 TS)", "https://windows.php.net/downloads/releases/php-8.4.1-Win32-vs16-x64.zip", "Najnowsze wydanie"),
                new("8.1.31", "PHP 8.1.31 (VS16 x64 TS)", "https://windows.php.net/downloads/releases/php-8.1.31-Win32-vs16-x64.zip", "Starsze wydanie")
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
                new("2.4.68", "Apache 2.4.68 (VS18 x64) - CKE", "https://www.apachelounge.com/download/VS18/binaries/httpd-2.4.68-260827-Win64-VS18.zip", "Oficjalne wydanie Apache 2.4 dla Windows", IsRecommended: true, IsCkeCompatible: true)
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
                new("10.4.32", "MariaDB 10.4.32 (winx64) - CKE", "https://archive.mariadb.org/mariadb-10.4.32/winx64-packages/mariadb-10.4.32-winx64.zip", "Wymagane przez CKE", IsRecommended: true, IsCkeCompatible: true),
                new("11.4.4", "MariaDB 11.4.4 (winx64)", "https://archive.mariadb.org/mariadb-11.4.4/winx64-packages/mariadb-11.4.4-winx64.zip", "Wydanie LTS (Długoterminowe)"),
                new("10.11.10", "MariaDB 10.11.10 (winx64)", "https://archive.mariadb.org/mariadb-10.11.10/winx64-packages/mariadb-10.11.10-winx64.zip", "Wydanie LTS")
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
                new("5.2.1", "phpMyAdmin 5.2.1 (All Languages) - CKE", "https://files.phpmyadmin.net/phpMyAdmin/5.2.1/phpMyAdmin-5.2.1-all-languages.zip", "Wymagane przez CKE", IsRecommended: true, IsCkeCompatible: true),
                new("5.2.2", "phpMyAdmin 5.2.2 (All Languages)", "https://files.phpmyadmin.net/phpMyAdmin/5.2.2/phpMyAdmin-5.2.2-all-languages.zip", "Najnowsze wydanie")
            ]
        }
    ];

    public static IReadOnlyList<(ComponentPackage Package, ComponentVersionInfo Version)> GetCkeProfile()
    {
        var apache = Packages.First(p => p.Id == "apache");
        var apacheVer = apache.Versions.First(v => v.IsCkeCompatible);

        var mariadb = Packages.First(p => p.Id == "mariadb");
        var mariadbVer = mariadb.Versions.First(v => v.IsCkeCompatible);

        var php = Packages.First(p => p.Id == "php");
        var phpVer = php.Versions.First(v => v.IsCkeCompatible);

        var pma = Packages.First(p => p.Id == "phpmyadmin");
        var pmaVer = pma.Versions.First(v => v.IsCkeCompatible);

        return [
            (apache, apacheVer),
            (mariadb, mariadbVer),
            (php, phpVer),
            (pma, pmaVer)
        ];
    }
}
