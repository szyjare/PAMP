using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace PAMP;

public sealed class UpdateService
{
    private static readonly UpdateService _instance = new();
    public static UpdateService Instance => _instance;

    private readonly HttpClient _httpClient;
    private const string GitHubApiUrl = "https://api.github.com/repos/szyjare/PAMP/releases/latest";
    private const string FallbackManifestUrl = "https://repo.sjarecki.pl/api/pamp-manifest";
    private const string InnoAppId = "{C18F1202-986D-46F2-8116-F1A9346E8203}_is1";

    public UpdateService()
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = true };
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"PAMP-App/{ManifestLoader.GetAppVersion()} (Windows NT 10.0; Win64; x64)");
    }

    public async Task<UpdatePackageInfo?> CheckForUpdatesAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        // Obsługa testowego mocka aktualizacji (flaga --mock-update lub zmienna środowiskowa PAMP_MOCK_UPDATE=1)
        bool isMock = Environment.GetCommandLineArgs().Any(arg =>
            string.Equals(arg, "--mock-update", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "-mock-update", StringComparison.OrdinalIgnoreCase)) ||
            Environment.GetEnvironmentVariable("PAMP_MOCK_UPDATE") == "1";

        if (isMock)
        {
            bool isInst = IsRunningInstalled();
            bool isSelf = IsRunningSelfContained();
            string mockAssetUrl = isInst
                ? "https://github.com/szyjare/PAMP/releases/download/v1.2.1/PAMP-Setup-1.2.1.exe"
                : "https://github.com/szyjare/PAMP/releases/download/v1.2.1/PAMP-v1.2.1-win-x64-portable.zip";

            return new UpdatePackageInfo
            {
                Version = "1.3.0",
                Title = "PAMP v1.3.0 — Wydanie testowe (Mock Update)",
                Changelog = "## 🚀 Co nowego w PAMP v1.3.0:\n\n- Wbudowany mechanizm sprawdzania i instalowania aktualizacji (in-app updater)\n- Automatyczna detekcja wydań z oficjalnego API GitHub Releases\n- Obsługa wersji instalacyjnej (Inno Setup) oraz przenośnej (Portable ZIP)\n- Pasek postępu pobierania w czasie rzeczywistym\n- Dedykowane okno dialogowe z pełnym changelogiem\n- Usprawnienia stabilności i wydajności",
                ReleaseUrl = "https://github.com/szyjare/PAMP/releases",
                SelectedAssetUrl = mockAssetUrl,
                SelectedAssetName = isInst ? "PAMP-Setup-1.3.0-mock.exe" : "PAMP-v1.3.0-mock.zip",
                SizeBytes = 2674435,
                IsInstaller = isInst,
                IsSelfContained = isSelf,
                IsMock = true
            };
        }

        if (!force)
        {
            if (!App.Settings.AutoCheckUpdates) return null;

            if (App.Settings.LastUpdateCheckUtc.HasValue)
            {
                var elapsed = DateTime.UtcNow - App.Settings.LastUpdateCheckUtc.Value;
                if (elapsed < TimeSpan.FromHours(24)) return null;
            }
        }

        GitHubReleaseDto? release = null;

        // 1. Primary: GitHub Releases API
        try
        {
            using var response = await _httpClient.GetAsync(GitHubApiUrl, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync(cancellationToken);
                release = JsonSerializer.Deserialize<GitHubReleaseDto>(json);
            }
        }
        catch
        {
            // Network or rate limit error on GitHub API
        }

        // 2. Fallback: PocketBase manifest (if GitHub API failed)
        if (release == null)
        {
            try
            {
                using var response = await _httpClient.GetAsync(FallbackManifestUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("pampRelease", out var pampRelElement))
                    {
                        release = JsonSerializer.Deserialize<GitHubReleaseDto>(pampRelElement.GetRawText());
                    }
                }
            }
            catch { }
        }

        if (release == null || string.IsNullOrWhiteSpace(release.TagName))
        {
            if (force)
            {
                throw new InvalidOperationException(TranslationSource.Instance["msgCantConnectToUpdateServer"] ?? "Nie można połączyć się z serwerem aktualizacji.");
            }
            return null;
        }

        // Clean version tag: "v1.2.1" -> "1.2.1"
        string remoteVersion = release.TagName.Trim().TrimStart('v', 'V');
        string currentVersion = ManifestLoader.GetAppVersion();

        // Save last check timestamp
        App.Settings.LastUpdateCheckUtc = DateTime.UtcNow;
        App.Settings.Save();

        // Check if user chose to skip this specific version (only when not checking manually)
        if (!force && !string.IsNullOrWhiteSpace(App.Settings.SkippedVersion) &&
            string.Equals(App.Settings.SkippedVersion, remoteVersion, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Compare versions
        int cmp = VersionComparer.Instance.Compare(remoteVersion, currentVersion);
        if (cmp <= 0)
        {
            // Current version is up to date or newer
            return null;
        }

        // Determine environment & installation type
        bool isInstalled = IsRunningInstalled();
        bool isSelfContained = IsRunningSelfContained();

        // Find suitable asset
        var selectedAsset = SelectAppropriateAsset(release.Assets, isInstalled, isSelfContained);
        if (selectedAsset == null)
        {
            // Fallback: any .exe or .zip
            selectedAsset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
        }

        if (selectedAsset == null) return null;

        return new UpdatePackageInfo
        {
            Version = remoteVersion,
            Title = !string.IsNullOrWhiteSpace(release.Name) ? release.Name : $"PAMP v{remoteVersion}",
            Changelog = !string.IsNullOrWhiteSpace(release.Body) ? release.Body.Trim() : "",
            ReleaseUrl = !string.IsNullOrWhiteSpace(release.HtmlUrl) ? release.HtmlUrl : "https://github.com/szyjare/PAMP/releases",
            SelectedAssetUrl = selectedAsset.BrowserDownloadUrl,
            SelectedAssetName = selectedAsset.Name,
            SizeBytes = selectedAsset.Size,
            IsInstaller = isInstalled || selectedAsset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase),
            IsSelfContained = isSelfContained
        };
    }

    public async Task DownloadAndApplyUpdateAsync(
        UpdatePackageInfo update,
        ServerService serverService,
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken = default)
    {
        string updateDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PAMP", "temp", "update");
        Directory.CreateDirectory(updateDir);

        string downloadedFilePath = Path.Combine(updateDir, update.SelectedAssetName);
        if (File.Exists(downloadedFilePath))
        {
            try { File.Delete(downloadedFilePath); } catch { }
        }

        // 1. Download asset with progress
        string downloadingText = TranslationSource.Instance["updateDownloading"] ?? "Pobieranie aktualizacji...";
        progress.Report(new InstallProgress(downloadingText, 0));

        if (update.IsMock)
        {
            long mockTotal = 25 * 1024 * 1024; // 25 MB
            for (int i = 0; i <= 100; i += 5)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(75, cancellationToken);
                double mbRead = (mockTotal * (i / 100.0)) / 1048576.0;
                double mbTotal = mockTotal / 1048576.0;
                progress.Report(new InstallProgress($"{downloadingText} {i}% ({mbRead:0.0} / {mbTotal:0.0} MB)", i));
            }

            string msg = TranslationSource.Instance["updateMockSuccessMessage"] ?? "Test pobierania i mechanizmu aktualizacji (v1.3.0 Mock) zakończony sukcesem!\n\nPasek postępu, raportowanie pobranych megabajtów oraz obsługa dialogu działają prawidłowo.";
            MessageBox.Show(
                msg,
                "PAMP — Mock Update",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        using (var response = await _httpClient.GetAsync(update.SelectedAssetUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
        {
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength ?? (update.SizeBytes > 0 ? update.SizeBytes : null);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(downloadedFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

            byte[] buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;

                if (totalBytes.HasValue && totalBytes.Value > 0)
                {
                    double percent = (double)totalRead / totalBytes.Value * 100.0;
                    double mbRead = totalRead / 1048576.0;
                    double mbTotal = totalBytes.Value / 1048576.0;
                    progress.Report(new InstallProgress($"{downloadingText} {percent:0}% ({mbRead:0.0} / {mbTotal:0.0} MB)", percent));
                }
                else
                {
                    double mbRead = totalRead / 1048576.0;
                    progress.Report(new InstallProgress($"{downloadingText} {mbRead:0.0} MB", 0, true));
                }
            }
        }

        // 2. Stop running services before updating
        string stoppingText = TranslationSource.Instance["updateStoppingServices"] ?? "Zatrzymywanie usług Apache i MariaDB...";
        progress.Report(new InstallProgress(stoppingText, 100, true));

        try
        {
            await serverService.StopAllAsync();
        }
        catch { }

        // 3. Apply update depending on package type
        string applyingText = TranslationSource.Instance["updateApplying"] ?? "Przygotowywanie instalacji...";
        progress.Report(new InstallProgress(applyingText, 100, true));

        if (update.IsInstaller)
        {
            // Standard interactive installer: user sees the setup window
            Process.Start(new ProcessStartInfo
            {
                FileName = downloadedFilePath,
                UseShellExecute = true
            });

            // Shutdown PAMP so installer can overwrite files
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window w in Application.Current.Windows)
                {
                    try { w.Hide(); } catch { }
                }
                Application.Current.Shutdown();
            });
            _ = Task.Delay(2000).ContinueWith(_ => Environment.Exit(0));
        }
        else
        {
            // Portable ZIP: extract and run in-place updater script
            string extractDir = Path.Combine(updateDir, "extracted");
            if (Directory.Exists(extractDir))
            {
                try { Directory.Delete(extractDir, true); } catch { }
            }
            Directory.CreateDirectory(extractDir);

            ZipFile.ExtractToDirectory(downloadedFilePath, extractDir, overwriteFiles: true);

            string currentBaseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            int currentPid = Environment.ProcessId;

            string scriptPath = Path.Combine(updateDir, "apply_update.cmd");
            string scriptContent =
                "@echo off\r\n" +
                "set EXTRACTED=" + extractDir + "\r\n" +
                "set PID=" + currentPid + "\r\n" +
                "set TARGET=" + currentBaseDir + "\r\n\r\n" +
                ":wait_loop\r\n" +
                "tasklist /fi \"PID eq %PID%\" 2>nul | find \"%PID%\" >nul\r\n" +
                "if not errorlevel 1 (\r\n" +
                "    timeout /t 1 /nobreak >nul\r\n" +
                "    goto wait_loop\r\n" +
                ")\r\n\r\n" +
                "xcopy /s /e /y /q \"%EXTRACTED%\\*\" \"%TARGET%\\\" >nul 2>&1\r\n" +
                "start \"\" \"%TARGET%\\PAMP.exe\"\r\n" +
                "rd /s /q \"%EXTRACTED%\" >nul 2>&1\r\n" +
                "(goto) 2>nul & del \"%~f0\"\r\n";

            await File.WriteAllTextAsync(scriptPath, scriptContent, cancellationToken);

            Process.Start(new ProcessStartInfo
            {
                FileName = scriptPath,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            // Shutdown PAMP so the script can copy files
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window w in Application.Current.Windows)
                {
                    try { w.Hide(); } catch { }
                }
                Application.Current.Shutdown();
            });
            _ = Task.Delay(2000).ContinueWith(_ => Environment.Exit(0));
        }
    }

    public static bool IsRunningInstalled()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');

        // Check if Inno Setup uninstaller exists in base directory
        if (File.Exists(Path.Combine(baseDir, "unins000.exe"))) return true;

        // Check registry for Inno Setup installation
        try
        {
            using var userKey = Registry.CurrentUser.OpenSubKey($@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{InnoAppId}");
            if (userKey?.GetValue("InstallLocation") is string userLoc &&
                string.Equals(userLoc.TrimEnd('\\'), baseDir, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            using var machineKey = Registry.LocalMachine.OpenSubKey($@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{InnoAppId}");
            if (machineKey?.GetValue("InstallLocation") is string machineLoc &&
                string.Equals(machineLoc.TrimEnd('\\'), baseDir, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        catch { }

        return false;
    }

    public static bool IsRunningSelfContained()
    {
        // When published as single-file self-contained, PAMP.dll is not present in the output folder
        string pampDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PAMP.dll");
        return !File.Exists(pampDll);
    }

    private static GitHubAssetDto? SelectAppropriateAsset(List<GitHubAssetDto> assets, bool isInstalled, bool isSelfContained)
    {
        if (assets.Count == 0) return null;

        if (isInstalled)
        {
            if (isSelfContained)
            {
                // PAMP-Setup-{version}-standalone.exe
                return assets.FirstOrDefault(a =>
                    a.Name.Contains("Setup", StringComparison.OrdinalIgnoreCase) &&
                    a.Name.Contains("standalone", StringComparison.OrdinalIgnoreCase) &&
                    a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    ?? assets.FirstOrDefault(a => a.Name.Contains("Setup", StringComparison.OrdinalIgnoreCase) && a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                // PAMP-Setup-{version}.exe
                return assets.FirstOrDefault(a =>
                    a.Name.Contains("Setup", StringComparison.OrdinalIgnoreCase) &&
                    !a.Name.Contains("standalone", StringComparison.OrdinalIgnoreCase) &&
                    a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    ?? assets.FirstOrDefault(a => a.Name.Contains("Setup", StringComparison.OrdinalIgnoreCase) && a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
            }
        }
        else
        {
            // Portable ZIP
            if (isSelfContained)
            {
                // PAMP-{version}-win-x64-standalone.zip
                return assets.FirstOrDefault(a =>
                    a.Name.Contains("standalone", StringComparison.OrdinalIgnoreCase) &&
                    a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    ?? assets.FirstOrDefault(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                // PAMP-{version}-win-x64-portable.zip
                return assets.FirstOrDefault(a =>
                    a.Name.Contains("portable", StringComparison.OrdinalIgnoreCase) &&
                    a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    ?? assets.FirstOrDefault(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
