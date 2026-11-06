using System;
using System.IO;

namespace PAMP;

/// <summary>
/// Centralny zarządca ścieżek aplikacji dla trybu instalatora oraz przenośnego (single-folder Portable).
/// </summary>
public static class AppPaths
{
    private static readonly Lazy<bool> _isPortable = new(() =>
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // Jeśli w folderze aplikacji znajduje się plik deinstalatora Inno Setup,
        // aplikacja została zainstalowana na komputerze (nie jest portable).
        if (File.Exists(Path.Combine(baseDir, "unins000.exe")))
            return false;

        // Tryb portable jest aktywowany, gdy w katalogu programu znajduje się plik .portable
        // lub portable.txt, albo folder data (samodzielny folder danych).
        return File.Exists(Path.Combine(baseDir, ".portable"))
            || File.Exists(Path.Combine(baseDir, "portable.txt"))
            || Directory.Exists(Path.Combine(baseDir, "data"));
    });

    /// <summary>
    /// Zwraca true, jeśli aplikacja działa w trybie przenośnym (single-folder portable).
    /// </summary>
    public static bool IsPortable => _isPortable.Value;

    /// <summary>
    /// Główny katalog wykonywalny aplikacji (gdzie znajduje się PAMP.exe).
    /// </summary>
    public static string AppDir => AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>
    /// Katalog z binariami komponentów (Apache, PHP, MariaDB, PMA).
    /// Zawsze znajduje się wewnątrz katalogu aplikacji: [AppDir]/bin
    /// </summary>
    public static string BinDir => Path.Combine(AppDir, "bin");

    /// <summary>
    /// Katalog danych aplikacji:
    /// - W trybie portable: [AppDir]/data
    /// - W trybie instalatora: %LOCALAPPDATA%/PAMP
    /// </summary>
    public static string DataDir => IsPortable
        ? Path.Combine(AppDir, "data")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PAMP");

    /// <summary>
    /// Katalog konfiguracji (httpd_user.conf, my_user.ini, settings.json).
    /// </summary>
    public static string ConfDir => Path.Combine(DataDir, "conf");

    /// <summary>
    /// Katalog logów serwerów (apache_error.log, mysql_error.log itp.).
    /// </summary>
    public static string LogsDir => Path.Combine(DataDir, "logs");

    /// <summary>
    /// Katalog bazy danych MariaDB (datadir).
    /// </summary>
    public static string MariaDbDataDir => Path.Combine(DataDir, "mysql_data");

    /// <summary>
    /// Katalog tymczasowy aplikacji (pobieranie pakietów, aktualizacji).
    /// </summary>
    public static string TempDir => Path.Combine(DataDir, "temp");

    /// <summary>
    /// Katalog sesji PHP dla phpMyAdmin.
    /// </summary>
    public static string SessionDir => Path.Combine(DataDir, "tmp");

    /// <summary>
    /// Główny katalog stron www (DocumentRoot dla Apache):
    /// - W trybie portable: [AppDir]/LocalSites
    /// - W trybie instalatora: %USERPROFILE%/PAMP/LocalSites
    /// </summary>
    public static string LocalSitesDir => IsPortable
        ? Path.Combine(AppDir, "LocalSites")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "PAMP", "LocalSites");

    /// <summary>
    /// Katalog bazowy użytkownika:
    /// - W trybie portable: [AppDir]
    /// - W trybie instalatora: %USERPROFILE%/PAMP
    /// </summary>
    public static string UserPampRoot => IsPortable
        ? AppDir
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "PAMP");

    /// <summary>
    /// Tworzy wymagane katalogi struktury danych aplikacji.
    /// </summary>
    public static void EnsureDirectoriesCreated()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            Directory.CreateDirectory(ConfDir);
            Directory.CreateDirectory(LogsDir);
            Directory.CreateDirectory(MariaDbDataDir);
            Directory.CreateDirectory(TempDir);
            Directory.CreateDirectory(SessionDir);
            Directory.CreateDirectory(LocalSitesDir);
        }
        catch { }
    }
}
