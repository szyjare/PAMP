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
            EnsurePhpConfiguration();
            EnsurePhpMyAdminConfiguration();
            CreateApacheConfig();
            CreateMariaDbConfig();
            CreateDefaultIndexPhp();
        }

        public void EnsurePhpConfiguration(string? customPhpDir = null)
        {
            string phpBinDir = customPhpDir ?? Path.Combine(_appBinDir, "bin", "php");
            if (!Directory.Exists(phpBinDir)) return;

            string iniPath = Path.Combine(phpBinDir, "php.ini");
            string iniDevPath = Path.Combine(phpBinDir, "php.ini-development");
            string iniProdPath = Path.Combine(phpBinDir, "php.ini-production");

            if (!File.Exists(iniPath))
            {
                if (File.Exists(iniDevPath))
                {
                    File.Copy(iniDevPath, iniPath, true);
                }
                else if (File.Exists(iniProdPath))
                {
                    File.Copy(iniProdPath, iniPath, true);
                }
                else
                {
                    File.WriteAllText(iniPath, "[PHP]\nextension_dir = \"ext\"\n");
                }
            }

            if (!File.Exists(iniPath)) return;

            string content = File.ReadAllText(iniPath);
            bool modified = false;

            // 1. Odkomentowanie extension_dir = "ext"
            if (System.Text.RegularExpressions.Regex.IsMatch(content, @"(?m)^;\s*extension_dir\s*=\s*""ext"""))
            {
                content = System.Text.RegularExpressions.Regex.Replace(content, @"(?m)^;\s*extension_dir\s*=\s*""ext""", "extension_dir = \"ext\"");
                modified = true;
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(content, @"(?m)^\s*extension_dir\s*="))
            {
                content += Environment.NewLine + "extension_dir = \"ext\"";
                modified = true;
            }

            // 2. Włączenie wtyczek zgodnych ze standardem XAMPP i wymogami CKE
            // Uwaga: mbstring musi być załadowany przed exif!
            string[] xamppExtensions =
            [
                "bz2",
                "curl",
                "fileinfo",
                "gd",
                "gettext",
                "intl",
                "mbstring",
                "exif",
                "mysqli",
                "openssl",
                "pdo_mysql",
                "pdo_sqlite",
                "soap",
                "sockets",
                "sqlite3",
                "tidy",
                "xsl",
                "zip"
            ];

            foreach (var ext in xamppExtensions)
            {
                // Sprawdzamy, czy dyrektywa jest zakomentowana: ;extension=ext
                if (System.Text.RegularExpressions.Regex.IsMatch(content, $@"(?m)^;extension={ext}\b"))
                {
                    content = System.Text.RegularExpressions.Regex.Replace(content, $@"(?m)^;extension={ext}\b", $"extension={ext}");
                    modified = true;
                }
                else if (!System.Text.RegularExpressions.Regex.IsMatch(content, $@"(?m)^extension={ext}\b"))
                {
                    content += Environment.NewLine + $"extension={ext}";
                    modified = true;
                }
            }

            // 3. Konfiguracja parametrów pod kątem developmentu i egzaminów CKE (pma/importy/timezone)
            var regexSettings = new (string Pattern, string Replacement)[]
            {
                (@"(?m)^;?\s*upload_max_filesize\s*=.*$", "upload_max_filesize = 128M"),
                (@"(?m)^;?\s*post_max_size\s*=.*$", "post_max_size = 128M"),
                (@"(?m)^;?\s*memory_limit\s*=.*$", "memory_limit = 512M"),
                (@"(?m)^;?\s*max_execution_time\s*=.*$", "max_execution_time = 300"),
                (@"(?m)^;?\s*max_input_time\s*=.*$", "max_input_time = 120"),
                (@"(?m)^;?\s*date\.timezone\s*=.*$", "date.timezone = Europe/Warsaw")
            };

            foreach (var (pattern, replacement) in regexSettings)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(content, pattern))
                {
                    string updated = System.Text.RegularExpressions.Regex.Replace(content, pattern, replacement);
                    if (updated != content)
                    {
                        content = updated;
                        modified = true;
                    }
                }
                else
                {
                    content += Environment.NewLine + replacement;
                    modified = true;
                }
            }

            if (modified)
            {
                File.WriteAllText(iniPath, content);
            }
        }

        public void EnsurePhpMyAdminConfiguration(string? customPmaDir = null)
        {
            string pmaBinDir = customPmaDir ?? Path.Combine(_appBinDir, "bin", "phpmyadmin");
            if (!Directory.Exists(pmaBinDir)) return;

            string configPath = Path.Combine(pmaBinDir, "config.inc.php");
            string langConfig = @"/* Domyślny język interfejsu (polski) z opcją zmiany przez użytkownika */
$cfg['DefaultLang'] = 'pl';
if (empty($_COOKIE['pma_lang']) && empty($_GET['lang']) && empty($_POST['lang'])) {
    $cfg['Lang'] = 'pl';
}";

            if (!File.Exists(configPath))
            {
                string pmaConfig = $@"<?php
declare(strict_types=1);

/**
 * PAMP - Konfiguracja phpMyAdmin
 * Profil w stylu XAMPP z logowaniem automatycznym (root bez hasła)
 */
{langConfig}

$cfg['blowfish_secret'] = 'pamp_secret_key_32_bytes_long_random_string_cke';

$i = 0;
$i++;
/* Authentication type */
$cfg['Servers'][$i]['auth_type'] = 'config';
$cfg['Servers'][$i]['user'] = 'root';
$cfg['Servers'][$i]['password'] = '';
$cfg['Servers'][$i]['host'] = '127.0.0.1';
$cfg['Servers'][$i]['port'] = '3306';
$cfg['Servers'][$i]['compress'] = false;
$cfg['Servers'][$i]['AllowNoPassword'] = true;

/* Storage database and tables */
$cfg['Servers'][$i]['pmadb'] = 'phpmyadmin';
$cfg['Servers'][$i]['bookmarktable'] = 'pma__bookmark';
$cfg['Servers'][$i]['relation'] = 'pma__relation';
$cfg['Servers'][$i]['table_info'] = 'pma__table_info';
$cfg['Servers'][$i]['table_coords'] = 'pma__table_coords';
$cfg['Servers'][$i]['pdf_pages'] = 'pma__pdf_pages';
$cfg['Servers'][$i]['column_info'] = 'pma__column_info';
$cfg['Servers'][$i]['history'] = 'pma__history';
$cfg['Servers'][$i]['table_uiprefs'] = 'pma__table_uiprefs';
$cfg['Servers'][$i]['tracking'] = 'pma__tracking';
$cfg['Servers'][$i]['userconfig'] = 'pma__userconfig';
$cfg['Servers'][$i]['recent'] = 'pma__recent';
$cfg['Servers'][$i]['favorite'] = 'pma__favorite';
$cfg['Servers'][$i]['users'] = 'pma__users';
$cfg['Servers'][$i]['usergroups'] = 'pma__usergroups';
$cfg['Servers'][$i]['navigationhiding'] = 'pma__navigationhiding';
$cfg['Servers'][$i]['savedsearches'] = 'pma__savedsearches';
$cfg['Servers'][$i]['central_columns'] = 'pma__central_columns';
$cfg['Servers'][$i]['designer_settings'] = 'pma__designer_settings';
$cfg['Servers'][$i]['export_templates'] = 'pma__export_templates';

$cfg['UploadDir'] = '';
$cfg['SaveDir'] = '';
";
                File.WriteAllText(configPath, pmaConfig);
            }
            else
            {
                // Jeśli config.inc.php już istnieje, upewnij się że zawiera ustawienia języka polskiego
                string current = File.ReadAllText(configPath);
                if (!current.Contains("DefaultLang") || !current.Contains("'pl'"))
                {
                    if (current.Contains("<?php"))
                    {
                        current = current.Replace("<?php", "<?php" + Environment.NewLine + langConfig + Environment.NewLine);
                    }
                    else
                    {
                        current = langConfig + Environment.NewLine + current;
                    }
                    File.WriteAllText(configPath, current);
                }
            }
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
            sb.AppendLine("ServerName localhost:80");
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
                "libcrypto-3-x64.dll",   // Nowsze PHP 8.2+
                "libssl-3-x64.dll",       // Nowsze PHP 8.2+
                "libcrypto-1_1-x64.dll", // Starsze PHP 8
                "libssl-1_1-x64.dll",    // Starsze PHP 8
                "libssh2.dll",
                "nghttp2.dll",
                "libsqlite3.dll",
                "libsodium.dll",
                "libpq.dll"
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
            string shareDir = Path.Combine(_appBinDir, "bin", "mariadb", "share").Replace("\\", "/");

            var sb = new StringBuilder();
            sb.AppendLine("[mysqld]");
            sb.AppendLine($"datadir=\"{dataDir}\"");
            sb.AppendLine("port=3306");
            sb.AppendLine($"socket=\"{socketPath}\"");
            // Ważne: ścieżka do share/charsets i messages w katalogu bin/mariadb/share
            sb.AppendLine($"lc-messages-dir=\"{shareDir}\"");
            sb.AppendLine("lc-messages=pl_PL");

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