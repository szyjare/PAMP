using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading; 
using System.Windows.Media;
using Microsoft.Win32;

namespace PAMP
{
    public partial class MainWindow : Window
    {
        private EnvironmentManager _envManager;
        private Process _apacheProcess;
        private Process _mysqlProcess;
        private bool _isRunning = false;
        private DispatcherTimer _statusTimer;

        private int _apachePort = 0;
        private int _mariaDbPort = 0;
        private bool _wasApacheRunning = false;
        private bool _wasMariaDbRunning = false;
        private bool isApacheRunning = false;
        private bool isMariaDbRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            _envManager = new EnvironmentManager();

            // Nasłuchiwanie na blokadę ekranu (Win+L)
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;

            InitializeStatusMonitor();
            LoadVersionsToUI();
        }

        // --- Version Loader ---
        private void LoadVersionsToUI()
        {
            var manifest = ManifestLoader.Load();

            if (manifest.Versions != null)
            {
                TxtPhpVersion.Text = $"{manifest.Versions.Php}";
                TxtApacheVersion.Text = $"{manifest.Versions.Apache}";
                TxtMariaDbVersion.Text = $"{manifest.Versions.MariaDb}";
                TxtPMAVersion.Text = $"{manifest.Versions.PhpMyAdmin}";
                TxtPampVersion.Text = $"Wersja: {manifest.Versions.Pamp}";

                this.Title = $"PAMP v{manifest.Versions.Pamp}";
            }
        }

        // --- Wyszukiwanie procesów ---
        private void InitializeStatusMonitor()
        {
            _statusTimer = new DispatcherTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(1); // Sprawdzaj co 1 sekundę
            _statusTimer.Tick += StatusTimer_Tick;
            _statusTimer.Start();

            var apacheProcess = Process.GetProcessesByName("httpd");
            isApacheRunning = apacheProcess.Length > 0;
            var dbProcess = Process.GetProcessesByName("mysqld");
            isMariaDbRunning = dbProcess.Length > 0;


            if ((isMariaDbRunning && _mysqlProcess == null) || (isApacheRunning && _mysqlProcess == null))
            {
                MessageBox.Show($"Wykryto uruchomione procesy Apache lub MySQL, które nie pochodzą od PAMP! \nW celu zapewnienia poprawnego działania programu zamknij pierw te procesy.", "PAMP! Ostrzeżenie", MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Shutdown();
            }
        }

        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            CheckServiceStatus();
        }

        private void CheckServiceStatus()
        {
            // Szukamy procesu "httpd"
            var apacheProcess = Process.GetProcessesByName("httpd");
            isApacheRunning = apacheProcess.Length > 0;
            UpdateDot(ApacheStatusDot, isApacheRunning);

            if (isApacheRunning)
            {
                // Jeśli właśnie się uruchomił (zmiana stanu z false na true) lub nie znamy portu
                if (!_wasApacheRunning || _apachePort == 0)
                {
                    // Pobieramy port (to może chwilę potrwać, więc normalnie robi się to async,
                    // ale tutaj przy jednym razie nie zablokuje mocno UI)
                    _apachePort = NetworkHelper.GetPortByPid(apacheProcess[0].Id);

                    // Ustawiamy ToolTip
                    apachePortBlock.Text = $"{_apachePort}";
                    apacheBtn.IsEnabled = true;
                    apacheBtn.Content = "Stop";
                }
            }
            else
            {
                apachePortBlock.Text = "...";
                _apachePort = 0;
                apacheBtn.Content = "Start";
                apacheBtn.IsEnabled = true;
            }
            _wasApacheRunning = isApacheRunning;

            // Szukamy procesu "mysqld"
            var dbProcess = Process.GetProcessesByName("mysqld");
            isMariaDbRunning = dbProcess.Length > 0;
            UpdateDot(DbStatusDot, isMariaDbRunning);

            if (isMariaDbRunning)
            {
                if (!_wasMariaDbRunning || _mariaDbPort == 0)
                {
                    _mariaDbPort = NetworkHelper.GetPortByPid(dbProcess[0].Id);
                    dbPortBlock.Text = $"{_mariaDbPort}";
                    mysqlBtn.IsEnabled = true;
                    mysqlBtn.Content = "Stop";
                }
            }
            else
            {
                dbPortBlock.Text = "...";
                _mariaDbPort = 0;
                mysqlBtn.Content = "Start";
                mysqlBtn.IsEnabled = true;
            }

            _wasMariaDbRunning = isMariaDbRunning;

    
        }

        private void UpdateDot(System.Windows.Shapes.Ellipse dot, bool isRunning)
        {
            if (isRunning)
            {
                dot.Fill = Brushes.LimeGreen;
            }
            else
            {
                dot.Fill = Brushes.Red;
            }
        }

        // --- Obsługa Przycisków ---

        private void BtnToggleServer_Click(object sender, RoutedEventArgs e)
        {
            var context = ((System.Windows.Controls.Button)sender).Tag;

            if (context.Equals("apache") && _apacheProcess == null)
            {
                StartPamp((string)context);
            }else if (context.Equals("apache") && _apacheProcess != null)
            {
                StopPamp((string)context);
            }

            if (context.Equals("mysql") && _mysqlProcess == null)
            {
                StartPamp((string)context);
            }else if (context.Equals("mysql") && _mysqlProcess != null)
            {
                StopPamp((string)context);
            }
        }

        private void BtnOpenLocalhost_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("http://localhost") { UseShellExecute = true });
            }
            catch 
            { 

            }
        }

        private void BtnOpenPma_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("http://localhost/phpmyadmin") { UseShellExecute = true });
            }
            catch
            {

            }
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "PAMP", "LocalSites");
            if (Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            else
            {
                MessageBox.Show("Folder jeszcze nie istnieje. Uruchom serwer przynajmniej raz.");
            }
        }

        private void BtnShowLogs_Click(object sender, RoutedEventArgs e)
        {
            var logWindow = new LogViewer();
            logWindow.Show();
        }

        private void BtnShowAbout_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new About(this);
            aboutWindow.Show();
        }

        private void BtnOpenSqlShell_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string mariadbBin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "mariadb", "bin");
                string mysqlExe = Path.Combine(mariadbBin, "mysql.exe");

                if (!File.Exists(mysqlExe))
                {
                    MessageBox.Show("Nie znaleziono pliku mysql.exe!");
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k chcp 65001 & cls & \"{mysqlExe}\" -u root --default-character-set=utf8mb4",
                    UseShellExecute = true,
                    WorkingDirectory = mariadbBin
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nie udało się otworzyć konsoli: " + ex.Message);
            }
        }

        private async void BtnFactoryReset_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "UWAGA! Ta operacja bezpowrotnie usunie bazę danych.\nKontynuować?",
                "Factory Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            StopPamp("all");

            bool stopped = await WaitForProcessToExitAsync("mysqld");

            if (!stopped)
            {
                MessageBox.Show("Nie udało się zamknąć MariaDB. Zrestartuj komputer i spróbuj ponownie.");
                return;
            }


            string mariadbBase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "mariadb", "bin");
            string appDataPamp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PAMP");
            string dataDir = Path.Combine(appDataPamp, "mysql_data");
            string installDbExe = Path.Combine(mariadbBase, "mysql_install_db.exe");

            bool deleted = false;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    if (Directory.Exists(dataDir))
                    {
                        Directory.Delete(dataDir, true);
                    }
                    deleted = true;
                    break;
                }
                catch (IOException)
                {
                    await Task.Delay(200);
                }
                catch (UnauthorizedAccessException)
                {
                    await Task.Delay(200);
                }
            }

            if (!deleted)
            {
                MessageBox.Show("Błąd: Nie można usunąć folderu 'data'. Pliki są nadal zablokowane przez system.");
                return;
            }

            await Task.Run(() =>
            {
                var procInfo = new ProcessStartInfo
                {
                    FileName = installDbExe,
                    Arguments = $"--datadir=\"{dataDir}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(installDbExe)
                };

                var proc = Process.Start(procInfo);
                proc.WaitForExit();
            });

            MessageBox.Show("Baza zresetowana pomyślnie!", "Sukces");
        }

        private void InitializePmaDatabase()
        {
            string mariadbBin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "mariadb", "bin");
            string mysqlExe = Path.Combine(mariadbBin, "mysql.exe");
            string sqlFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "phpmyadmin", "sql", "create_tables.sql");

            // Walidacja plików
            if (!File.Exists(mysqlExe) || !File.Exists(sqlFile)) return;
            bool isDbReady = false;
            int maxRetries = 20;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var pingProc = Process.Start(new ProcessStartInfo
                    {
                        FileName = mysqlExe,
                        Arguments = "-u root --skip-password -e \"SELECT 1\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });

                    pingProc.WaitForExit();

                    if (pingProc.ExitCode == 0)
                    {
                        isDbReady = true;
                        break; 
                    }
                }
                catch { }

                System.Threading.Thread.Sleep(500);
            }

            if (!isDbReady)
            {
                Debug.WriteLine("Timeout: MariaDB nie wstała w ciągu 10 sekund.");
                return;
            }

            try
            {
                var checkProc = Process.Start(new ProcessStartInfo
                {
                    FileName = mysqlExe,
                    Arguments = "-u root --skip-password -e \"USE phpmyadmin\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                checkProc.WaitForExit();

                if (checkProc.ExitCode != 0) 
                {
                    var importInfo = new ProcessStartInfo
                    {
                        FileName = mysqlExe,
                        Arguments = "-u root --skip-password",
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(importInfo))
                    {
                        using (StreamReader reader = new StreamReader(sqlFile))
                        {
                            process.StandardInput.Write(reader.ReadToEnd());
                        }
                        process.StandardInput.Close();
                        process.WaitForExit();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Błąd inicjalizacji PMA: " + ex.Message);
            }
        }

        // --- Logika Start / Stop ---
        private void StartApache()
        {
            apacheBtn.IsEnabled = false;
            string apacheExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "apache", "bin", "httpd.exe");
            string phpDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "php");

            var apacheInfo = new ProcessStartInfo
            {
                FileName = apacheExe,
                Arguments = $"-f \"{_envManager.ApacheConfigPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            string currentPath = Environment.GetEnvironmentVariable("PATH");
            
            apacheInfo.EnvironmentVariables["PATH"] = phpDir + ";" + Environment.GetEnvironmentVariable("PATH");

            _apacheProcess = Process.Start(apacheInfo);
        }

        private void StartMariaDB()
        {
            mysqlBtn.IsEnabled = false;
            string mysqlExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "mariadb", "bin", "mysqld.exe");
            var mysqlInfo = new ProcessStartInfo
            {
                FileName = mysqlExe,
                Arguments = $"--defaults-file=\"{_envManager.MariaDbConfigPath}\" --console",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            _mysqlProcess = Process.Start(mysqlInfo);
            InitializePmaDatabase();
        }

        private void StopApache()
        {
            apacheBtn.IsEnabled = false;
            if (_apacheProcess != null && !_apacheProcess.HasExited)
            {
                try { _apacheProcess.Kill(); } catch { }
                _apacheProcess = null;
            }
        }

        private void StopMariaDB()
        {
            mysqlBtn.IsEnabled = false;
            if (_mysqlProcess != null && !_mysqlProcess.HasExited)
            {
                string mariadbBin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "mariadb", "bin");
                string mysqlAdmin = Path.Combine(mariadbBin, "mysqladmin.exe");

                if (File.Exists(mysqlAdmin))
                {
                    var shutdownInfo = new ProcessStartInfo
                    {
                        FileName = mysqlAdmin,
                        Arguments = "-u root shutdown",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    try
                    {
                        var proc = Process.Start(shutdownInfo);
                        proc.WaitForExit(5000);
                    }
                    catch { }
                }

                if (!_mysqlProcess.HasExited)
                {
                    try { _mysqlProcess.Kill(); } catch { }
                }

                _mysqlProcess = null;
            }
        }

        private void StartPamp(String context)
        {
            try
            {
                // 1. Generowanie konfigów
                _envManager.InitializeEnvironment();

                // Automatyczna inicjalizacja bazy danych
                string appDataPamp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PAMP");
                string mysqlDataDir = Path.Combine(appDataPamp, "mysql_data");

                // Sprawdzamy czy istnieje kluczowy folder systemowy "mysql" wewnątrz mysql_data
                if (!Directory.Exists(Path.Combine(mysqlDataDir, "mysql")))
                {

                    // Ścieżka do instalatora
                    string mariadbBin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "mariadb", "bin");
                    string installDbExe = Path.Combine(mariadbBin, "mysql_install_db.exe");

                    // Zabezpieczenie na wypadek braku pliku
                    if (!File.Exists(installDbExe))
                    {
                        throw new FileNotFoundException("Brakuje pliku mysql_install_db.exe w folderze bin/mariadb/bin!");
                    }

                    var initInfo = new ProcessStartInfo
                    {
                        FileName = installDbExe,
                        Arguments = $"--datadir=\"{mysqlDataDir}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    // Uruchom i czekaj aż skończy
                    var proc = Process.Start(initInfo);
                    proc.WaitForExit();

                    if (proc.ExitCode != 0)
                    {
                        throw new Exception($"Inicjalizacja bazy zakończona błędem (Kod: {proc.ExitCode}).");
                    }
                }

                if (context.Equals("apache"))
                {
                    StartApache();
                }
                else if (context.Equals("mysql"))
                {
                    StartMariaDB();
                }

                _isRunning = true;

                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas uruchamiania: {ex.Message}");
                StopPamp("all");
            }
        }

        private void StopPamp(String context)
        {
            try
            {

                if (context.Equals("apache"))
                {
                    StopApache();
                }
                else if (context.Equals("mysql"))
                {
                    StopMariaDB();
                }
                else
                {
                    StopApache();
                    StopMariaDB();
                }

                _isRunning = (_apacheProcess != null && !_apacheProcess.HasExited) | (_mysqlProcess != null && !_mysqlProcess.HasExited);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas zamykania: {ex.Message}");
            }
        }

        // --- Blokada Ekranu ---
        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                Dispatcher.Invoke(() =>
                {
                    if (_isRunning)
                    {
                        Console.WriteLine("Blokada ekranu - autozamykanie serwerów.");
                        StopPamp("all");
                    }
                });
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            StopPamp("all");
            Application.Current.Shutdown();
        }

        private async Task<bool> WaitForProcessToExitAsync(string processName, int timeoutSeconds = 10)
        {
            var startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
            {
                var processes = Process.GetProcessesByName(processName);

                if (processes.Length == 0)
                {
                    return true;
                }

                await Task.Delay(100);
            }

            return false;
        }

        protected override void OnClosed(EventArgs e)
        {
            _statusTimer?.Stop();

            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            if (_isRunning) StopPamp("all");
            base.OnClosed(e);
        }
    }
}