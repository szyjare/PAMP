using System.Diagnostics;
using System.IO;

namespace PAMP;

public sealed class ServerService : IDisposable
{
    private readonly EnvironmentManager _envManager = new();
    private Process? _apacheProcess;
    private Process? _mysqlProcess;

    public bool IsApacheRunning => _apacheProcess is { HasExited: false };
    public bool IsMariaDbRunning => _mysqlProcess is { HasExited: false };

    public async Task<bool> CheckForeignProcessesRunningAsync()
    {
        return await Task.Run(() =>
        {
            var httpdProcs = Process.GetProcessesByName("httpd");
            bool hasForeignApache = httpdProcs.Length > 0 && _apacheProcess is null;
            foreach (var p in httpdProcs) p.Dispose();

            var mysqlProcs = Process.GetProcessesByName("mysqld");
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

            var mysqlProcs = Process.GetProcessesByName("mysqld");
            bool mysqlActive = mysqlProcs.Length > 0;
            int mysqlPid = mysqlActive ? mysqlProcs[0].Id : 0;
            foreach (var p in mysqlProcs) p.Dispose();

            int apachePort = apacheActive ? await NetworkHelper.GetPortByPidAsync(apachePid) : 0;
            int mysqlPort = mysqlActive ? await NetworkHelper.GetPortByPidAsync(mysqlPid) : 0;

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
    }

    public async Task StartMariaDbAsync()
    {
        if (IsMariaDbRunning) return;

        _envManager.InitializeEnvironment();
        await EnsureDatabaseInitializedAsync();

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string mysqlExe = Path.Combine(baseDir, "bin", "mariadb", "bin", "mysqld.exe");

        var startInfo = new ProcessStartInfo
        {
            FileName = mysqlExe,
            Arguments = $"--defaults-file=\"{_envManager.MariaDbConfigPath}\" --console",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _mysqlProcess = Process.Start(startInfo);
        await InitializePmaDatabaseAsync();
    }

    public async Task StopMariaDbAsync()
    {
        if (_mysqlProcess is { HasExited: false })
        {
            string mariadbBin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "mariadb", "bin");
            string mysqlAdmin = Path.Combine(mariadbBin, "mysqladmin.exe");

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

            if (_mysqlProcess is { HasExited: false })
            {
                try { _mysqlProcess.Kill(entireProcessTree: true); } catch { }
            }

            _mysqlProcess.Dispose();
            _mysqlProcess = null;
        }
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
            string mariadbBin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "mariadb", "bin");
            string installDbExe = Path.Combine(mariadbBin, "mysql_install_db.exe");

            if (!File.Exists(installDbExe))
                throw new FileNotFoundException("Brakuje pliku mysql_install_db.exe!");

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
        string mysqlExe = Path.Combine(baseDir, "bin", "mariadb", "bin", "mysql.exe");
        string sqlFile = Path.Combine(baseDir, "bin", "phpmyadmin", "sql", "create_tables.sql");

        if (!File.Exists(mysqlExe) || !File.Exists(sqlFile)) return;

        bool isReady = false;
        for (int i = 0; i < 20; i++)
        {
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

        if (!isReady) return;

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
