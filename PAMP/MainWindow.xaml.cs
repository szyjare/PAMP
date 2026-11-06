using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace PAMP;

public partial class MainWindow : Window
{
    private static readonly BitmapImage IconOn = CreateFrozenBitmap("pack://application:,,,/Resources/on.png");
    private static readonly BitmapImage IconOff = CreateFrozenBitmap("pack://application:,,,/Resources/off.png");

    private readonly ServerService _serverService = new();
    public ServerService ServerService => _serverService;

    private readonly DispatcherTimer _statusTimer = new();
    private bool _isCheckingStatus;
    private UpdatePackageInfo? _pendingUpdate;

    public MainWindow()
    {
        InitializeComponent();

        SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        ContentRendered += MainWindow_ContentRendered;

        SetupTimer();
        LoadVersionsToUI();
        TranslationSource.Instance.PropertyChanged += (_, _) => Dispatcher.Invoke(LoadVersionsToUI);
        _ = ComponentCatalog.EnsureLoadedAsync();
        _ = CheckInitialProcessesAsync();
    }

    private static BitmapImage CreateFrozenBitmap(string uri)
    {
        var bmp = new BitmapImage(new Uri(uri, UriKind.Absolute));
        bmp.Freeze();
        return bmp;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        EnableMicaBackdrop();
        UpdateTitleBarTheme();
    }

    private void EnableMicaBackdrop() => App.EnableMicaBackdrop(this);

    public void UpdateTitleBarTheme() => App.UpdateTitleBarTheme(this);

    private void MainWindow_ContentRendered(object? sender, EventArgs e)
    {
        ContentRendered -= MainWindow_ContentRendered;
        VCRedistHelper.CheckAndPrompt(this);

        if (AdminHelper.IsAdministrator)
        {
            _ = Task.Run(() =>
            {
                ComponentInstallerService.EnsureDirectoryPermissions(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin"));
                string progData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PAMP");
                ComponentInstallerService.EnsureDirectoryPermissions(progData);
            });
        }

        FirstRunCheck();

        bool openComponents = Environment.GetCommandLineArgs().Any(arg =>
            string.Equals(arg, "--manage-components", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "--components", StringComparison.OrdinalIgnoreCase));

        if (openComponents)
        {
            var compWindow = new ComponentManagerWindow(this, _serverService, LoadVersionsToUI);
            compWindow.ShowDialog();
        }

        _ = CheckUpdatesOnStartupAsync();
    }

    private void FirstRunCheck()
    {
        bool isFirstRun = App.Settings.FirstRun;
        bool hasAnyComponents = ManifestLoader.HasAnyComponentInstalled();

        if (isFirstRun)
        {
            App.Settings.FirstRun = false;
            App.Settings.Save();
        }

        if (ManifestLoader.AreEssentialComponentsInstalled())
        {
            // Wszystkie wymagane pakiety są już zainstalowane
            return;
        }

        // Jeśli to pierwsze uruchomienie LUB na komputerze w ogóle brak zainstalowanych komponentów PAMP:
        if (isFirstRun || !hasAnyComponents)
        {
            if (!AdminHelper.CanManageComponents)
            {
                var result = MessageBox.Show(
                    this,
                    TranslationSource.Instance["compAdminRequiredFirstRun"] ?? "PAMP nie posiada jeszcze zainstalowanych komponentów (Apache, PHP, MariaDB).\n\nDo ich zainstalowania wymagane są uprawnienia administratora.\n\nCzy chcesz uruchomić program teraz jako administrator, aby otworzyć instalator komponentów?",
                    TranslationSource.Instance["compAdminRequiredTitle"] ?? "Wymagane uprawnienia administratora",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    AdminHelper.RestartAsAdministrator("--manage-components");
                    return;
                }
            }

            var compWindow = new ComponentManagerWindow(this, _serverService, LoadVersionsToUI);
            compWindow.ShowDialog();
        }
    }

    private void SetupTimer()
    {
        _statusTimer.Interval = TimeSpan.FromSeconds(2);
        _statusTimer.Tick += async (_, _) => await RefreshStatusAsync();
        _statusTimer.Start();
    }

    private async Task CheckInitialProcessesAsync()
    {
        if (await _serverService.CheckForeignProcessesRunningAsync())
        {
            MessageBox.Show(
                TranslationSource.Instance["msgProcessDetected"],
                TranslationSource.Instance["pampWarning"],
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Application.Current.Shutdown();
            return;
        }

        await RefreshStatusAsync();
    }

    private async Task RefreshStatusAsync()
    {
        if (_isCheckingStatus) return;
        _isCheckingStatus = true;

        try
        {
            var (apacheActive, apachePort, mysqlActive, mysqlPort) = await _serverService.GetStatusSnapshotAsync();

            UpdateServiceUI(ApacheStatusIcon, apacheBtn, apachePortBlock, apacheActive, apachePort);
            UpdateServiceUI(DbStatusIcon, mysqlBtn, dbPortBlock, mysqlActive, mysqlPort);

            // Aktualizacja dostępności przycisków i pozycji menu
            btnOpenLocalhost.IsEnabled = apacheActive;
            menuToolsLocalhost.IsEnabled = apacheActive;

            btnOpenSqlShell.IsEnabled = mysqlActive;
            menuToolsSQL.IsEnabled = mysqlActive;

            btnOpenPma.IsEnabled = apacheActive && mysqlActive;
            menuToolsPMA.IsEnabled = apacheActive && mysqlActive;
        }
        finally
        {
            _isCheckingStatus = false;
        }
    }

    private static void UpdateServiceUI(Image icon, Button btn, TextBlock portBlock, bool isRunning, int port)
    {
        icon.Source = isRunning ? IconOn : IconOff;

        btn.Content = TranslationSource.Instance[isRunning ? "stop" : "start"];
        btn.IsEnabled = true;
        portBlock.Text = isRunning && port > 0 ? port.ToString() : (isRunning ? "..." : "-");
    }

    private async void BtnToggleServer_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string serviceTag } btn) return;

        btn.IsEnabled = false;

        try
        {
            switch (serviceTag)
            {
                case "apache":
                    if (_serverService.IsApacheRunning)
                        await _serverService.StopApacheAsync();
                    else
                    {
                        if (!VCRedistHelper.CheckAndPrompt(this)) return;
                        await _serverService.StartApacheAsync();
                    }
                    break;

                case "mysql":
                    if (_serverService.IsMariaDbRunning)
                        await _serverService.StopMariaDbAsync();
                    else
                    {
                        if (!VCRedistHelper.CheckAndPrompt(this)) return;
                        try
                        {
                            await _serverService.StartMariaDbAsync();
                        }
                        catch (Exception ex)
                        {
                            string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PAMP", "mysql_data");
                            if (Directory.Exists(dataDir))
                            {
                                var result = MessageBox.Show(
                                    $"{TranslationSource.Instance["msgMariaDbIncompatiblePrompt"]}\n\n{ex.Message}",
                                    TranslationSource.Instance["pampWarning"],
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Warning);

                                if (result == MessageBoxResult.Yes)
                                {
                                    _statusTimer.Stop();
                                    OverlayLoading.Visibility = Visibility.Visible;
                                    TxtLoadingMessage.Text = TranslationSource.Instance["msgDbWipingInProgress"];
                                    try
                                    {
                                        bool resetSuccess = await _serverService.ResetDatabaseAsync();
                                        if (resetSuccess)
                                        {
                                            await _serverService.StartMariaDbAsync();
                                        }
                                        else
                                        {
                                            MessageBox.Show(TranslationSource.Instance["msgCantDeleteDbData"], TranslationSource.Instance["pampWarning"], MessageBoxButton.OK, MessageBoxImage.Error);
                                        }
                                    }
                                    finally
                                    {
                                        OverlayLoading.Visibility = Visibility.Collapsed;
                                        _statusTimer.Start();
                                    }
                                    return;
                                }
                            }

                            throw;
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            string failMsg = TranslationSource.Instance["msgFailedToStart"] + ex.Message;
            MessageBox.Show(failMsg, TranslationSource.Instance["pampWarning"], MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            await RefreshStatusAsync();
        }
    }

    private void LoadVersionsToUI()
    {
        var manifest = ManifestLoader.Load();
        if (manifest.Versions is not { } v) return;

        TxtPhpVersion.Text = v.Php;
        TxtApacheVersion.Text = v.Apache;
        TxtMariaDbVersion.Text = v.MariaDb;
        TxtPMAVersion.Text = v.PhpMyAdmin;
        RunPampVersionNumber.Text = $" {v.Pamp}";
        Title = $"PAMP v{v.Pamp}";
    }

    private void BtnShowSettings_Click(object sender, RoutedEventArgs e) =>
        new Settings(this).ShowDialog();

    private void BtnShowComponents_Click(object sender, RoutedEventArgs e)
    {
        var compWindow = new ComponentManagerWindow(this, _serverService, LoadVersionsToUI);
        compWindow.ShowDialog();
    }

    private void BtnOpenLocalhost_Click(object sender, RoutedEventArgs e) =>
        OpenBrowser("http://localhost");

    private void BtnOpenPma_Click(object sender, RoutedEventArgs e) =>
        OpenBrowser("http://localhost/phpmyadmin");

    private static void OpenBrowser(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }

    private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
    {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "PAMP", "LocalSites");
        if (Directory.Exists(path))
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        else
            MessageBox.Show(TranslationSource.Instance["msgDirectoryNotExisting"], TranslationSource.Instance["pampWarning"], MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private LogViewer? _logViewer;

    private void BtnShowLogs_Click(object sender, RoutedEventArgs e)
    {
        if (_logViewer != null && _logViewer.IsLoaded)
        {
            if (_logViewer.WindowState == WindowState.Minimized)
                _logViewer.WindowState = WindowState.Normal;
            _logViewer.Activate();
            return;
        }

        _logViewer = new LogViewer { Owner = this };
        _logViewer.Closed += (_, _) => _logViewer = null;
        _logViewer.Show();
    }

    private void BtnShowAbout_Click(object sender, RoutedEventArgs e) => new About(this).ShowDialog();

    private void BtnOpenSqlShell_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string binDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "mariadb", "bin");
            string mysqlExe = Path.Combine(binDir, "mysql.exe");

            if (!File.Exists(mysqlExe))
            {
                MessageBox.Show(TranslationSource.Instance["msgMysqlExeNotFound"], TranslationSource.Instance["pampWarning"], MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k chcp 65001 & cls & \"{mysqlExe}\" -u root --default-character-set=utf8mb4",
                UseShellExecute = true,
                WorkingDirectory = binDir
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(TranslationSource.Instance["msgCantOpenConsole"] + ex.Message, TranslationSource.Instance["pampWarning"], MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnFactoryReset_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(
            TranslationSource.Instance["msgDbWipeAsk"],
            TranslationSource.Instance["pampWarning"],
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        _statusTimer.Stop();
        OverlayLoading.Visibility = Visibility.Visible;
        TxtLoadingMessage.Text = TranslationSource.Instance["msgDbWipingInProgress"];
        bool success = false;
        try
        {
            success = await _serverService.ResetDatabaseAsync();
        }
        finally
        {
            OverlayLoading.Visibility = Visibility.Collapsed;
            _statusTimer.Start();
        }

        if (success)
            MessageBox.Show(TranslationSource.Instance["msgDbWiped"], TranslationSource.Instance["pampSuccess"], MessageBoxButton.OK, MessageBoxImage.Information);
        else
            MessageBox.Show(TranslationSource.Instance["msgCantDeleteDbData"], TranslationSource.Instance["pampWarning"], MessageBoxButton.OK, MessageBoxImage.Error);

        await RefreshStatusAsync();
    }

    private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionLock)
        {
            _ = Task.Run(async () => await _serverService.StopAllAsync());
        }
    }

    private async Task CheckUpdatesOnStartupAsync()
    {
        try
        {
            var update = await UpdateService.Instance.CheckForUpdatesAsync(force: false);
            if (update != null)
            {
                _pendingUpdate = update;
                string template = TranslationSource.Instance["updateBannerNewVersion"] ?? "Dostępna nowa wersja: PAMP v{0}!";
                TxtBannerUpdateMessage.Text = string.Format(template, update.Version);
                BannerUpdate.Visibility = Visibility.Visible;

                if (update.IsMock)
                {
                    new UpdateDialog(this, update, _serverService).ShowDialog();
                }
            }
        }
        catch { }
    }

    private async void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var update = await UpdateService.Instance.CheckForUpdatesAsync(force: true);
            if (update != null)
            {
                _pendingUpdate = update;
                new UpdateDialog(this, update, _serverService).ShowDialog();
            }
            else
            {
                string msg = string.Format(
                    TranslationSource.Instance["updateUpToDate"] ?? "Masz najnowszą wersję PAMP ({0}).",
                    ManifestLoader.GetAppVersion());
                MessageBox.Show(this, msg, "PAMP", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            string errPrefix = TranslationSource.Instance["updateCheckError"] ?? "Błąd sprawdzania aktualizacji:\n";
            MessageBox.Show(this, $"{errPrefix}{ex.Message}", TranslationSource.Instance["pampWarning"] ?? "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnBannerUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingUpdate != null)
        {
            new UpdateDialog(this, _pendingUpdate, _serverService).ShowDialog();
        }
    }

    private void BtnBannerDismiss_Click(object sender, RoutedEventArgs e)
    {
        BannerUpdate.Visibility = Visibility.Collapsed;
    }

    private async void Exit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _serverService.StopAllAsync();
        }
        catch { }
        finally
        {
            Application.Current.Shutdown();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _statusTimer.Stop();
        SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
        _serverService.Dispose();
        base.OnClosed(e);
    }
}
