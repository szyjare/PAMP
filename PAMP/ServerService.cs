using System.Diagnostics;
using System.IO;

namespace PAMP;

public sealed class ServerService : IDisposable
{
    private readonly EnvironmentManager _envManager = new();
    private Process? _apacheProcess;
    private Process? _mysqlProcess;

    // Cache portów per PID - eliminacja ciągłego wywoływania procesu netstat
    private int _cachedApachePid;
    private int _cachedApachePort;
    private int _cachedMysqlPid;
    private int _cachedMysqlPort;

    private static Process[] GetMariaDbProcesses()
    {
        var p1 = Process.GetProcessesByName("mysqld");
        var p2 = Process.GetProcessesByName("mariadbd");
        return p1.Concat(p2).ToArray();
    }

    public bool IsApacheRunning => _apacheProcess is { HasExited: false } || Process.GetProcessesByName("httpd").Length > 0;
    public bool IsMariaDbRunning
    {
        get
        {
            if (_mysqlProcess is { HasExited: false }) return true;
            var procs = GetMariaDbProcesses();
            bool running = procs.Length > 0;
            foreach (var p in procs) p.Dispose();
            return running;
        }
    }

    public async Task<bool> CheckForeignProcessesRunningAsync()
    {
        return await Task.Run(() =>
        {
            var httpdProcs = Process.GetProcessesByName("httpd");
            bool hasForeignApache = httpdProcs.Length > 0 && _apacheProcess is null;
            foreach (var p in httpdProcs) p.Dispose();

            var mysqlProcs = GetMariaDbProcesses();
            bool hasForeignMysql = mysqlProcs.Length > 0 && _mysqlProcess is null;
            foreach (var p in mysqlProcs) p.Dispose();

            return hasForeignApache || hasForeignMysql;
        });
    }

    public async Task<(bool ApacheRunning, int ApachePort, bool MariaDbRunning, int MariaDbPort)> GetStatusSnapshotAsync()
    {
        return await Task.Run(async () =>
        {
            var apacheProcs = Process.GetProcessesByName("httpd");
            bool apacheActive = apacheProcs.Length > 0;
            int apachePid = apacheActive ? apacheProcs[0].Id : 0;
            foreach (var p in apacheProcs) p.Dispose();

            var mysqlProcs = GetMariaDbProcesses();
            bool mysqlActive = mysqlProcs.Length > 0;
            int mysqlPid = mysqlActive ? mysqlProcs[0].Id : 0;
            foreach (var p in mysqlProcs) p.Dispose();

            int apachePort = 0;
            if (apacheActive)
            {
                if (_cachedApachePid != apachePid || _cachedApachePort == 0)
                {
                    _cachedApachePort = await NetworkHelper.GetPortByPidAsync(apachePid);
                    _cachedApachePid = apachePid;
                }
                apachePort = _cachedApachePort;
            }
            else
            {
                _cachedApachePid = 0;
                _cachedApachePort = 0;
            }

            int mysqlPort = 0;
            if (mysqlActive)
            {
                if (_cachedMysqlPid != mysqlPid || _cachedMysqlPort == 0)
                {
                    _cachedMysqlPort = await NetworkHelper.GetPortByPidAsync(mysqlPid);
                    _cachedMysqlPid = mysqlPid;
                }
                mysqlPort = _cachedMysqlPort;
            }
            else
            {
                _cachedMysqlPid = 0;
                _cachedMysqlPort = 0;
            }

            return (apacheActive, apachePort, mysqlActive, mysqlPort);
        });
    }

    public async Task StartApacheAsync()
    {
        if (IsApacheRunning) return;

        _envManager.InitializeEnvironment();
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string apacheExe = Path.Combine(baseDir, "bin", "apache", "bin", "httpd.exe");
        string phpDir = Path.Combine(baseDir, "bin", "php");

        var startInfo = new ProcessStartInfo
        {
            FileName = apacheExe,
            Arguments = $"-f \"{_envManager.ApacheConfigPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.EnvironmentVariables["PATH"] = phpDir + ";" + Environment.GetEnvironmentVariable("PATH");
        _apacheProcess = Process.Start(startInfo);
    }

    public async Task StopApacheAsync()
    {
        if (_apacheProcess is { HasExited: false })
        {
            try
            {
                _apacheProcess.Kill(entireProcessTree: true);
                await _apacheProcess.WaitForExitAsync();
            }
            catch { }
            finally
            {
                _apacheProcess.Dispose();
                _apacheProcess = null;
            }
        }
        else
        {
            foreach (var p in Process.GetProcessesByName("httpd"))
            {
                try { p.Kill(entireProcessTree: true); } catch { }
                p.Dispose();
            }
        }

        _cachedApachePid = 0;
        _cachedApachePort = 0;
    }

    private string GetMariaDbServerExe()
    {
        string mariadbBin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "mariadb", "bin");
        string mariadbd = Path.Combine(mariadbBin, "mariadbd.exe");
        if (File.Exists(mariadbd)) return mariadbd;
        return Path.Combine(mariadbBin, "mysqld.exe");
    }

    private string GetMariaDbAdminExe()
    {
        string mariadbBin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "mariadb", "bin");
        string mariadbAdmin = Path.Combine(mariadbBin, "mariadb-admin.exe");
        if (File.Exists(mariadbAdmin)) return mariadbAdmin;
        return Path.Combine(mariadbBin, "mysqladmin.exe");
    }

    private string GetMariaDbClientExe()
    {
        string mariadbBin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "mariadb", "bin");
        string mariadbCli = Path.Combine(mariadbBin, "mariadb.exe");
        if (File.Exists(mariadbCli)) return mariadbCli;
        return Path.Combine(mariadbBin, "mysql.exe");
    }

    private string GetMariaDbInstallDbExe()
    {
        string mariadbBin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "mariadb", "bin");
        string mariadbInstall = Path.Combine(mariadbBin, "mariadb-install-db.exe");
        if (File.Exists(mariadbInstall)) return mariadbInstall;
        return Path.Combine(mariadbBin, "mysql_install_db.exe");
    }

    private string? GetMariaDbUpgradeExe()
    {
        string mariadbBin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "mariadb", "bin");
        string mariadbUpgrade = Path.Combine(mariadbBin, "mariadb-upgrade.exe");
        if (File.Exists(mariadbUpgrade)) return mariadbUpgrade;
        string mysqlUpgrade = Path.Combine(mariadbBin, "mysql_upgrade.exe");
        if (File.Exists(mysqlUpgrade)) return mysqlUpgrade;
        return null;
    }

    public async Task StartMariaDbAsync()
    {
        if (IsMariaDbRunning) return;

        _envManager.InitializeEnvironment();
        await EnsureDatabaseInitializedAsync();

        string mysqlExe = GetMariaDbServerExe();
        if (!File.Exists(mysqlExe))
            throw new FileNotFoundException($"Nie znaleziono pliku serwera MariaDB: {mysqlExe}");

        var startInfo = new ProcessStartInfo
        {
            FileName = mysqlExe,
            Arguments = $"--defaults-file=\"{_envManager.MariaDbConfigPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        };

        _mysqlProcess = Process.Start(startInfo);
        if (_mysqlProcess is null)
            throw new InvalidOperationException("Nie udało się uruchomić procesu MariaDB.");

        // Sprawdzamy czy proces nie wyłożył się natychmiast po uruchomieniu
        await Task.Delay(1000);
        if (_mysqlProcess.HasExited)
        {
            string errOutput = await _mysqlProcess.StandardError.ReadToEndAsync();
            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PAMP", "logs", "mysql_error.log");
            string errorDetails = "";
            if (File.Exists(logPath))
            {
                var lines = await File.ReadAllLinesAsync(logPath);
                var errLines = lines.Where(l => l.Contains("[ERROR]") || l.Contains("Aborting")).TakeLast(6).ToList();
                if (errLines.Count == 0) errLines = lines.TakeLast(6).ToList();
                errorDetails = string.Join(Environment.NewLine, errLines);
            }

            string combined = !string.IsNullOrWhiteSpace(errOutput) ? errOutput : errorDetails;
            throw new InvalidOperationException($"Serwer MariaDB zakończył działanie z kodem {_mysqlProcess.ExitCode}:\n\n{combined}");
        }

        await InitializePmaDatabaseAsync();
        await RunDatabaseUpgradeAsync();
    }

    public async Task StopMariaDbAsync()
    {
        string mysqlAdmin = GetMariaDbAdminExe();

        if (File.Exists(mysqlAdmin))
        {
            try
            {
                using var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = mysqlAdmin,
                    Arguments = "-u root shutdown",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                if (proc is not null)
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await proc.WaitForExitAsync(cts.Token);
                }
            }
            catch { }
        }

        // Czekamy aż proces zamknie się czysto i zapisze checkpoint InnoDB
        if (_mysqlProcess is { HasExited: false })
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                await _mysqlProcess.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { _mysqlProcess.Kill(entireProcessTree: true); } catch { }
            }
            finally
            {
                _mysqlProcess.Dispose();
                _mysqlProcess = null;
            }
        }

        foreach (var p in GetMariaDbProcesses())
        {
            try
            {
                if (!p.HasExited) p.Kill(entireProcessTree: true);
            }
            catch { }
            p.Dispose();
        }

        _cachedMysqlPid = 0;
        _cachedMysqlPort = 0;
    }

    private async Task RunDatabaseUpgradeAsync()
    {
        string? upgradeExe = GetMariaDbUpgradeExe();
        if (upgradeExe is null || !File.Exists(upgradeExe)) return;

        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = upgradeExe,
                Arguments = $"--defaults-file=\"{_envManager.MariaDbConfigPath}\" -u root --skip-password --force",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (proc is not null)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await proc.WaitForExitAsync(cts.Token);
            }
        }
        catch { }
    }

    public async Task StopAllAsync()
    {
        await Task.WhenAll(StopApacheAsync(), StopMariaDbAsync());
    }

    private async Task EnsureDatabaseInitializedAsync()
    {
        string appDataPamp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PAMP");
        string mysqlDataDir = Path.Combine(appDataPamp, "mysql_data");

        if (!Directory.Exists(Path.Combine(mysqlDataDir, "mysql")))
        {
            string installDbExe = GetMariaDbInstallDbExe();

            if (!File.Exists(installDbExe))
                throw new FileNotFoundException($"Brakuje pliku instalatora bazy MariaDB ({installDbExe})!");

            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = installDbExe,
                Arguments = $"--datadir=\"{mysqlDataDir}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (proc is not null)
            {
                await proc.WaitForExitAsync();
                if (proc.ExitCode != 0)
                    throw new InvalidOperationException($"Inicjalizacja bazy danych zakończona kodem {proc.ExitCode}");
            }
        }
    }

    private async Task InitializePmaDatabaseAsync()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string mysqlExe = GetMariaDbClientExe();
        string sqlFile = Path.Combine(baseDir, "bin", "phpmyadmin", "sql", "create_tables.sql");

        if (!File.Exists(mysqlExe) || !File.Exists(sqlFile)) return;

        bool isReady = false;
        for (int i = 0; i < 20; i++)
        {
            if (_mysqlProcess is { HasExited: true }) break;

            try
            {
                using var ping = Process.Start(new ProcessStartInfo
                {
                    FileName = mysqlExe,
                    Arguments = "-u root --skip-password -e \"SELECT 1\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (ping is not null)
                {
                    await ping.WaitForExitAsync();
                    if (ping.ExitCode == 0)
                    {
                        isReady = true;
                        break;
                    }
                }
            }
            catch { }

            await Task.Delay(500);
        }

        if (!isReady)
        {
            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PAMP", "logs", "mysql_error.log");
            string errorDetails = "";
            if (File.Exists(logPath))
            {
                var lines = await File.ReadAllLinesAsync(logPath);
                var errLines = lines.Where(l => l.Contains("[ERROR]") || l.Contains("Aborting")).TakeLast(6).ToList();
                if (errLines.Count == 0) errLines = lines.TakeLast(6).ToList();
                errorDetails = string.Join(Environment.NewLine, errLines);
            }

            if (_mysqlProcess is { HasExited: true })
            {
                throw new InvalidOperationException($"Serwer MariaDB wyłączył się podczas oczekiwania na gotowość (kod {_mysqlProcess.ExitCode}):\n\n{errorDetails}");
            }

            throw new TimeoutException($"Przekroczono limit czasu oczekiwania na gotowość bazy MariaDB.\n\n{errorDetails}");
        }

        await Task.Run(async () =>
        {
            try
            {
                using var checkProc = Process.Start(new ProcessStartInfo
                {
                    FileName = mysqlExe,
                    Arguments = "-u root --skip-password -e \"USE phpmyadmin\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (checkProc is not null)
                {
                    await checkProc.WaitForExitAsync();
                    if (checkProc.ExitCode != 0)
                    {
                        using var importProc = Process.Start(new ProcessStartInfo
                        {
                            FileName = mysqlExe,
                            Arguments = "-u root --skip-password",
                            UseShellExecute = false,
                            RedirectStandardInput = true,
                            CreateNoWindow = true
                        });

                        if (importProc is not null)
                        {
                            string sql = await File.ReadAllTextAsync(sqlFile);
                            await importProc.StandardInput.WriteAsync(sql);
                            importProc.StandardInput.Close();
                            await importProc.WaitForExitAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd inicjalizacji PMA: {ex.Message}");
            }
        });
    }

    public async Task<bool> ResetDatabaseAsync()
    {
        await StopAllAsync();

        string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PAMP", "mysql_data");
        bool deleted = false;

        for (int i = 0; i < 5; i++)
        {
            try
            {
                if (Directory.Exists(dataDir)) Directory.Delete(dataDir, true);
                deleted = true;
                break;
            }
            catch
            {
                await Task.Delay(250);
            }
        }

        if (!deleted) return false;

        await EnsureDatabaseInitializedAsync();
        return true;
    }

    public void Dispose()
    {
        _apacheProcess?.Dispose();
        _mysqlProcess?.Dispose();
    }
}
