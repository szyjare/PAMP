using System;
using System.IO;
using System.Text;

namespace PAMP
{
    public class EnvironmentManager
    {
        // Ukryty folder systemowy (AppData/Local/PAMP) - na bazę i configi
        private readonly string _systemDir;

        // Widoczny folder użytkownika (C:/Users/.../PAMP)
        private readonly string _userPampRoot;

        // Folder na strony (C:/Users/.../PAMP/LocalSites)
        private readonly string _htdocsDir;

        // Folder gdzie leży Twój plik .exe i binarki (Program Files/PAMP/bin)
        private readonly string _appBinDir;

        // Publiczne właściwości, żeby Launcher wiedział gdzie co jest
        public string ApacheConfigPath => Path.Combine(_systemDir, "conf", "httpd_user.conf");
        public string MariaDbConfigPath => Path.Combine(_systemDir, "conf", "my_user.ini");

        public EnvironmentManager()
        {
            // 1. Definicja ścieżek
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _systemDir = Path.Combine(localAppData, "PAMP");

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _userPampRoot = Path.Combine(userProfile, "PAMP");
            _htdocsDir = Path.Combine(_userPampRoot, "LocalSites");

            _appBinDir = AppDomain.CurrentDomain.BaseDirectory;
        }

        public void InitializeEnvironment()
        {
            EnsureDirectories();
            CreateApacheConfig();
            CreateMariaDbConfig();
            CreateDefaultIndexPhp();
        }

        private void EnsureDirectories()
        {
            // Tworzenie widocznej struktury PAMP/LocalSites
            if (!Directory.Exists(_htdocsDir))
            {
                Directory.CreateDirectory(_htdocsDir);
                // Plik README.txt dla użytkownika
                File.WriteAllText(Path.Combine(_userPampRoot, "CZYTAJ_TO.txt"),
                    "W folderze LocalSites umieszczaj swoje pliki PHP i HTML.");
            }

            // Tworzenie ukrytej struktury systemowej w AppData
            Directory.CreateDirectory(Path.Combine(_systemDir, "mysql_data"));
            Directory.CreateDirectory(Path.Combine(_systemDir, "conf"));
            Directory.CreateDirectory(Path.Combine(_systemDir, "logs"));
        }

        private void CreateApacheConfig()
        {
            // Zamiana backslashy na slashe (wymóg Apache)
            string rootDir = _appBinDir.Replace("\\", "/").TrimEnd('/');
            string sysDirSlash = _systemDir.Replace("\\", "/").TrimEnd('/');
            string htdocsSlash = _htdocsDir.Replace("\\", "/").TrimEnd('/');

            var sb = new StringBuilder();

            sb.AppendLine($"ServerRoot \"{rootDir}/bin/apache\"");
            sb.AppendLine("Listen 80");

            // --- Ładowanie modułów ---
            sb.AppendLine("LoadModule access_compat_module modules/mod_access_compat.so");
            sb.AppendLine("LoadModule authz_core_module modules/mod_authz_core.so");
            sb.AppendLine("LoadModule authz_host_module modules/mod_authz_host.so");
            sb.AppendLine("LoadModule log_config_module modules/mod_log_config.so");
            sb.AppendLine("LoadModule mime_module modules/mod_mime.so");
            sb.AppendLine("LoadModule dir_module modules/mod_dir.so");
            sb.AppendLine("LoadModule rewrite_module modules/mod_rewrite.so"); // Bardzo ważne dla WordPress/Laravel!
            sb.AppendLine("LoadModule alias_module modules/mod_alias.so");
            sb.AppendLine("LoadModule autoindex_module modules/mod_autoindex.so");
            // PHP Config
            string phpDir = $"{rootDir}/bin/php";
            string phpExtDir = $"{phpDir}/ext";
            string[] dependencyDlls = new string[]
            {
                "libssh2.dll",
                "libpq.dll",
                "libsqlite3.dll",
                "libcrypto-1_1-x64.dll", // Starsze PHP 8
                "libssl-1_1-x64.dll",    // Starsze PHP 8
                "libcrypto-3-x64.dll",   // Nowsze PHP 8.2+
                "libssl-3-x64.dll"       // Nowsze PHP 8.2+
            };

            foreach (var dllName in dependencyDlls)
            {
                // Sprawdzamy fizycznie, czy plik istnieje w folderze bin/php
                // (używamy _appBinDir, bo to lokalna ścieżka na dysku, a nie string do configu)
                string localDllPath = Path.Combine(_appBinDir, "bin", "php", dllName);

                if (File.Exists(localDllPath))
                {
                    // Jeśli istnieje, dopisujemy LoadFile do configu Apache
                    sb.AppendLine($"LoadFile \"{phpDir}/{dllName}\"");
                }
            }

            sb.AppendLine($"LoadModule php_module \"{rootDir}/bin/php/php8apache2_4.dll\"");
            sb.AppendLine("AddHandler application/x-httpd-php .php");
            sb.AppendLine($"PHPIniDir \"{rootDir}/bin/php\"");
            sb.AppendLine($"php_admin_value extension_dir \"{phpExtDir}\"");

            // --- KLUCZOWE: Wskazanie na folder PAMP/LocalSites ---
            sb.AppendLine($"DocumentRoot \"{htdocsSlash}\"");
            sb.AppendLine($"<Directory \"{htdocsSlash}\">");
            sb.AppendLine("    Options Indexes FollowSymLinks");
            sb.AppendLine("    AllowOverride All");
            sb.AppendLine("    Require all granted");
            sb.AppendLine("    DirectoryIndex index.php index.html");
            sb.AppendLine("</Directory>");

            // Logi i PID w AppData (żeby nie śmiecić w PAMP)
            sb.AppendLine($"PidFile \"{sysDirSlash}/conf/httpd.pid\"");
            sb.AppendLine($"ErrorLog \"{sysDirSlash}/logs/apache_error.log\"");
            sb.AppendLine("LogFormat \"%h %l %u %t \\\"%r\\\" %>s %b \\\"%{Referer}i\\\" \\\"%{User-Agent}i\\\"\" combined");
            sb.AppendLine($"CustomLog \"{sysDirSlash}/logs/apache_access.log\" combined");

            // =================================================================
            // NOWOŚĆ: Integracja phpMyAdmin
            // =================================================================
            string pmaPath = $"{rootDir}/bin/phpmyadmin";
            string tmpDir = $"{sysDirSlash}/tmp"; // Folder na sesje PHP

            // Tworzymy folder tmp w AppData, żeby sesje PMA miały gdzie się zapisywać
            // Bez tego PMA często wywala błąd "Session start failed"
            Directory.CreateDirectory(Path.Combine(_systemDir, "tmp"));

            sb.AppendLine($"# --- phpMyAdmin Configuration ---");
            sb.AppendLine($"Alias /phpmyadmin \"{pmaPath}\"");
            sb.AppendLine($"<Directory \"{pmaPath}\">");
            sb.AppendLine("    Options Indexes FollowSymLinks");
            sb.AppendLine("    AllowOverride All");
            sb.AppendLine("    Require all granted");

            // Ważne: Wymuszamy wersję PHP i ustawienia dla PMA
            sb.AppendLine("    DirectoryIndex index.php");

            // Fix na sesje (zapisujemy je w AppData usera, a nie w systemowym Temp)
            sb.AppendLine($"    php_admin_value session.save_path \"{tmpDir}\"");
            sb.AppendLine($"    php_admin_value upload_max_filesize 128M");
            sb.AppendLine($"    php_admin_value post_max_size 128M");
            sb.AppendLine("</Directory>");
            // =================================================================

            // Logi (bez zmian)
            sb.AppendLine($"PidFile \"{sysDirSlash}/conf/httpd.pid\"");

            File.WriteAllText(ApacheConfigPath, sb.ToString());
        }

        private void CreateMariaDbConfig()
        {
            string dataDir = Path.Combine(_systemDir, "mysql_data").Replace("\\", "/");
            string socketPath = Path.Combine(_systemDir, "conf", "mysql.sock").Replace("\\", "/");
            string pluginDir = Path.Combine(_appBinDir, "bin", "mariadb", "lib", "plugin").Replace("\\", "/");

            var sb = new StringBuilder();
            sb.AppendLine("[mysqld]");
            sb.AppendLine($"datadir=\"{dataDir}\"");
            sb.AppendLine("port=3306");
            sb.AppendLine($"socket=\"{socketPath}\"");
            // Ważne: ścieżka do share/charsets i messages, inaczej MariaDB może nie wstać po przeniesieniu
            sb.AppendLine($"lc-messages-dir=\"{_appBinDir.Replace("\\", "/")}/mariadb/share\"");

            sb.AppendLine("sql_mode=NO_ENGINE_SUBSTITUTION");
            sb.AppendLine($"log-error=\"{_systemDir.Replace("\\", "/")}/logs/mysql_error.log\"");

            File.WriteAllText(MariaDbConfigPath, sb.ToString());
        }

        private void CreateDefaultIndexPhp()
        {
            string indexPath = Path.Combine(_htdocsDir, "index.php");
            // Tworzymy plik powitalny tylko jeśli folder jest pusty lub plik nie istnieje
            if (!File.Exists(indexPath))
            {
                string welcomeHtml = @"
                    <!DOCTYPE html>
                    <html lang='pl'>
                    <head><title>Witaj w PAMP!</title></head>
                    <body style='font-family: sans-serif; text-align: center; padding-top: 50px;'>
                        <h1 style='color: #4CAF50;'>PAMP działa!</h1>
                        <p>Twój folder to: " + _htdocsDir + @"</p>
                        <hr>
                        <?php phpinfo(); ?>
                    </body>
                    </html>";

                File.WriteAllText(indexPath, welcomeHtml);
            }
        }
    }
}