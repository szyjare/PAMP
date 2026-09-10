using System.IO;
using System.IO.Compression;
using System.Net.Http;

namespace PAMP;

public record InstallProgress(string StatusText, double Percentage, bool IsIndeterminate = false);

public sealed class ComponentInstallerService
{
    private static readonly HttpClient HttpClient;

    static ComponentInstallerService()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true
        };
        HttpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(15)
        };
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    public async Task DownloadAndInstallAsync(
        ComponentPackage package,
        ComponentVersionInfo versionInfo,
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken = default)
    {
        string appDataTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PAMP", "temp");
        Directory.CreateDirectory(appDataTemp);

        string zipPath = Path.Combine(appDataTemp, $"{package.Id}_{versionInfo.Version}.zip");
        string extractDir = Path.Combine(appDataTemp, $"{package.Id}_{versionInfo.Version}_extracted");

        try
        {
            // 1. Pobieranie pliku
            progress.Report(new InstallProgress($"Pobieranie {versionInfo.DisplayName}...", 0));

            using (var response = await HttpClient.GetAsync(versionInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                // Weryfikacja typu zawartości: czy serwer nie zwrócił strony błędu HTML
                var mediaType = response.Content.Headers.ContentType?.MediaType;
                if (mediaType != null && mediaType.Contains("html", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Serwer zwrócił stronę HTML zamiast archiwum ZIP. Adres URL może być nieaktualny: {versionInfo.DownloadUrl}");
                }

                long? totalBytes = response.Content.Headers.ContentLength;
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

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
                        progress.Report(new InstallProgress($"Pobieranie: {percent:0}% ({mbRead:0.0} / {mbTotal:0.0} MB)", percent));
                    }
                    else
                    {
                        double mbRead = totalRead / 1048576.0;
                        progress.Report(new InstallProgress($"Pobrano: {mbRead:0.0} MB", 0, true));
                    }
                }
            }

            // Weryfikacja nagłówka ZIP (Magic Bytes: PK)
            using (var checkStream = File.OpenRead(zipPath))
            {
                byte[] header = new byte[4];
                int read = checkStream.Read(header, 0, 4);
                if (read < 4 || header[0] != 0x50 || header[1] != 0x4B)
                {
                    throw new InvalidOperationException($"Pobrany plik nie jest prawidłowym archiwum ZIP (brak nagłówka PK). Rozmiar: {new FileInfo(zipPath).Length} bajtów.");
                }
            }

            // 2. Rozpakowywanie archiwum
            progress.Report(new InstallProgress("Rozpakowywanie archiwum...", 100, true));

            if (Directory.Exists(extractDir))
            {
                Directory.Delete(extractDir, true);
            }
            Directory.CreateDirectory(extractDir);

            await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, extractDir, true), cancellationToken);

            // 3. Wykrycie folderu źródłowego
            string sourceDir = extractDir;

            if (package.Id == "apache")
            {
                // Archiwum Apache zawiera podfolder "Apache24"
                string apache24Dir = Path.Combine(extractDir, "Apache24");
                if (Directory.Exists(apache24Dir))
                {
                    sourceDir = apache24Dir;
                }
            }
            else
            {
                var subDirs = Directory.GetDirectories(extractDir);
                if (subDirs.Length == 1)
                {
                    sourceDir = subDirs[0];
                }
                else if (subDirs.Length > 1)
                {
                    var matching = subDirs.FirstOrDefault(d =>
                    {
                        string name = Path.GetFileName(d);
                        return name.StartsWith("mariadb", StringComparison.OrdinalIgnoreCase) ||
                               name.StartsWith("phpMyAdmin", StringComparison.OrdinalIgnoreCase);
                    });

                    if (matching != null)
                    {
                        sourceDir = matching;
                    }
                }
            }

            // 4. Podmiana w folderze bin aplikacji (BEZ NARUSZANIA BAZ DANYCH W %LocalAppData%\PAMP\mysql_data!)
            progress.Report(new InstallProgress("Instalacja plików w katalogu bin...", 100, true));

            string targetBinDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", package.TargetSubfolder);

            await Task.Run(() =>
            {
                if (Directory.Exists(targetBinDir))
                {
                    try
                    {
                        Directory.Delete(targetBinDir, true);
                    }
                    catch
                    {
                        string trashDir = Path.Combine(appDataTemp, $"trash_{Guid.NewGuid():N}");
                        Directory.Move(targetBinDir, trashDir);
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(targetBinDir)!);
                Directory.Move(sourceDir, targetBinDir);

                // Automatyczna konfiguracja środowiska (php.ini, wtyczki XAMPP, phpMyAdmin config)
                var env = new EnvironmentManager();
                if (package.Id == "php")
                {
                    env.EnsurePhpConfiguration(targetBinDir);
                }
                else if (package.Id == "phpmyadmin")
                {
                    env.EnsurePhpMyAdminConfiguration(targetBinDir);
                }
            }, cancellationToken);

            // 5. Aktualizacja manifest.json
            progress.Report(new InstallProgress("Aktualizacja konfiguracji wersji...", 100, true));

            var manifest = ManifestLoader.Load();
            switch (package.Id)
            {
                case "php":
                    manifest.Versions.Php = versionInfo.Version;
                    break;
                case "apache":
                    manifest.Versions.Apache = versionInfo.Version;
                    break;
                case "mariadb":
                    manifest.Versions.MariaDb = versionInfo.Version;
                    break;
                case "phpmyadmin":
                    manifest.Versions.PhpMyAdmin = versionInfo.Version;
                    break;
            }
            ManifestLoader.Save(manifest);

            progress.Report(new InstallProgress("Zakończono pomyślnie!", 100));
        }
        finally
        {
            // Czyszczenie tymczasowych plików
            try
            {
                if (File.Exists(zipPath)) File.Delete(zipPath);
                if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
            }
            catch { }
        }
    }

    public async Task InstallCkeProfileAsync(
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken = default)
    {
        var items = ComponentCatalog.GetCkeProfile();
        int total = items.Count;
        int current = 0;

        foreach (var (package, versionInfo) in items)
        {
            current++;
            int stepIndex = current;
            var subProgress = new Progress<InstallProgress>(p =>
            {
                double stepBase = (double)(stepIndex - 1) / total * 100.0;
                double stepWeight = 100.0 / total;
                double overallPercent = stepBase + (p.Percentage / 100.0 * stepWeight);
                progress.Report(new InstallProgress($"[{stepIndex}/{total}] {package.Name}: {p.StatusText}", overallPercent, p.IsIndeterminate));
            });

            await DownloadAndInstallAsync(package, versionInfo, subProgress, cancellationToken);
        }

        progress.Report(new InstallProgress("Pakiet CKE został pomyślnie zainstalowany!", 100));
    }
}
