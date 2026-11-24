using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;

namespace PAMP;

public partial class ComponentManagerWindow : Window
{
    private readonly ServerService _serverService;
    private readonly ComponentInstallerService _installerService = new();
    private readonly Action? _onInstalledCallback;
    private bool _isBusy;
    private readonly Dictionary<Button, int> _copyFeedbackTokens = [];

    public ComponentManagerWindow(Window? owner, ServerService serverService, Action? onInstalledCallback = null)
    {
        if (owner != null && owner.IsVisible)
            Owner = owner;
        else
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        _serverService = serverService;
        _onInstalledCallback = onInstalledCallback;

        InitializeComponent();
        LoadCurrentVersions();
        PopulateAvailableVersions();

        Loaded += async (_, _) =>
        {
            if (AdminHelper.IsAdministrator)
            {
                _ = Task.Run(() => ComponentInstallerService.EnsureDirectoryPermissions(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin")));
            }

            TxtRepoStatus.Text = TranslationSource.Instance.Get("compRepoSyncing", "repo.sjarecki.pl (synchronizacja...)");
            bool success = await ComponentCatalog.EnsureLoadedAsync();
            TxtRepoStatus.Text = success ? "✓ repo.sjarecki.pl" : TranslationSource.Instance.Get("compRepoOffline", "repo.sjarecki.pl (offline)");
            PopulateAvailableVersions();
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        EnableMicaBackdrop();
        UpdateTitleBarTheme();
    }

    private void EnableMicaBackdrop() => App.EnableMicaBackdrop(this);

    public void UpdateTitleBarTheme() => App.UpdateTitleBarTheme(this);

    private void LoadCurrentVersions()
    {
        BannerNonAdmin.Visibility = Visibility.Collapsed;
        CmbPhpVersions.IsEnabled = true;
        CmbApacheVersions.IsEnabled = true;
        CmbMariaDbVersions.IsEnabled = true;
        CmbPmaVersions.IsEnabled = true;

        var manifest = ManifestLoader.Load();
        if (manifest.Versions is not { } v) return;

        TxtPhpInstalled.Text = v.Php;
        TxtApacheInstalled.Text = v.Apache;
        TxtMariaDbInstalled.Text = v.MariaDb;
        TxtPmaInstalled.Text = v.PhpMyAdmin;
    }

    private void PopulateAvailableVersions()
    {
        BannerNonAdmin.Visibility = Visibility.Collapsed;
        CmbPhpVersions.IsEnabled = true;
        CmbApacheVersions.IsEnabled = true;
        CmbMariaDbVersions.IsEnabled = true;
        CmbPmaVersions.IsEnabled = true;

        var manifest = ManifestLoader.Load();
        var installed = manifest.Versions;

        foreach (var pkg in ComponentCatalog.Packages)
        {
            var cmb = pkg.Id switch
            {
                "php" => CmbPhpVersions,
                "apache" => CmbApacheVersions,
                "mariadb" => CmbMariaDbVersions,
                "phpmyadmin" => CmbPmaVersions,
                _ => null
            };

            if (cmb is null) continue;

            string? currentInstalled = pkg.Id switch
            {
                "php" => installed?.Php,
                "apache" => installed?.Apache,
                "mariadb" => installed?.MariaDb,
                "phpmyadmin" => installed?.PhpMyAdmin,
                _ => null
            };

            cmb.SelectionChanged -= CmbVersions_SelectionChanged;

            var previousSelectedVer = (cmb.SelectedItem as ComponentVersionInfo)?.Version;
            cmb.ItemsSource = null;
            cmb.ItemsSource = pkg.Versions;

            ComponentVersionInfo? toSelect = null;

            // 1. Priorytet: aktualnie zainstalowana wersja w PAMP
            if (!string.IsNullOrWhiteSpace(currentInstalled))
            {
                toSelect = pkg.Versions.FirstOrDefault(v =>
                    string.Equals(NormalizeVersion(v.Version), NormalizeVersion(currentInstalled), StringComparison.OrdinalIgnoreCase));
            }

            // 2. Jeśli brak dopasowania z zainstalowaną, a użytkownik miał wcześniej wybraną opcję
            if (toSelect == null && previousSelectedVer != null)
            {
                toSelect = pkg.Versions.FirstOrDefault(v =>
                    string.Equals(NormalizeVersion(v.Version), NormalizeVersion(previousSelectedVer), StringComparison.OrdinalIgnoreCase));
            }

            // 3. Fallback: profil XAMPP lub najnowsza wersja na górze
            toSelect ??= pkg.Versions.FirstOrDefault(v => v.IsXamppCompatible) ?? pkg.Versions.FirstOrDefault();
            cmb.SelectedItem = toSelect;

            cmb.SelectionChanged += CmbVersions_SelectionChanged;
        }

        UpdateActionButtons();
    }

    private void CmbVersions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateActionButtons();
    }

    private void UpdateActionButtons()
    {
        if (_isBusy) return;

        UpdateAllSha256Displays();

        if (!AdminHelper.CanManageComponents)
        {
            ApplyNonAdminRestrictions();
            return;
        }

        BannerNonAdmin.Visibility = Visibility.Collapsed;
        CmbPhpVersions.IsEnabled = true;
        CmbApacheVersions.IsEnabled = true;
        CmbMariaDbVersions.IsEnabled = true;
        CmbPmaVersions.IsEnabled = true;

        var manifest = ManifestLoader.Load();
        var installed = manifest.Versions;

        UpdateSinglePackageButton(CmbPhpVersions, BtnInstallPhp, installed?.Php);
        UpdateSinglePackageButton(CmbApacheVersions, BtnInstallApache, installed?.Apache);
        UpdateSinglePackageButton(CmbMariaDbVersions, BtnInstallMariaDb, installed?.MariaDb);
        UpdateSinglePackageButton(CmbPmaVersions, BtnInstallPma, installed?.PhpMyAdmin);

        UpdateXamppButton(installed);
        UpdateLatestButton(installed);
    }

    private void UpdateAllSha256Displays()
    {
        UpdateSha256Display(CmbPhpVersions, PnlPhpSha256, TxtPhpSha256, BtnCopyPhpSha256);
        UpdateSha256Display(CmbApacheVersions, PnlApacheSha256, TxtApacheSha256, BtnCopyApacheSha256);
        UpdateSha256Display(CmbMariaDbVersions, PnlMariaDbSha256, TxtMariaDbSha256, BtnCopyMariaDbSha256);
        UpdateSha256Display(CmbPmaVersions, PnlPmaSha256, TxtPmaSha256, BtnCopyPmaSha256);
    }

    private static void UpdateSha256Display(ComboBox cmb, StackPanel panel, TextBlock txt, Button btn)
    {
        string? rawHash = (cmb.SelectedItem as ComponentVersionInfo)?.Sha256?.Trim();
        if (!string.IsNullOrWhiteSpace(rawHash))
        {
            string normalizedHash = rawHash.ToLowerInvariant();
            panel.Visibility = Visibility.Visible;
            txt.Text = normalizedHash.Length > 24 ? $"{normalizedHash[..12]}…{normalizedHash[^12..]}" : normalizedHash;
            txt.ToolTip = normalizedHash;
            btn.Tag = normalizedHash;
        }
        else
        {
            panel.Visibility = Visibility.Collapsed;
            txt.Text = "";
            txt.ToolTip = null;
            btn.Tag = null;
        }
    }

    private async void BtnCopySha256_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string fullHash } btn || string.IsNullOrWhiteSpace(fullHash))
            return;

        try
        {
            Clipboard.SetText(fullHash);
        }
        catch
        {
            return;
        }

        if (btn.Content is TextBlock iconText)
        {
            int token = _copyFeedbackTokens.GetValueOrDefault(btn) + 1;
            _copyFeedbackTokens[btn] = token;

            iconText.Text = "\uE73E";
            iconText.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
            btn.ToolTip = TranslationSource.Instance["compSha256Copied"];

            await Task.Delay(1500);

            if (_copyFeedbackTokens.GetValueOrDefault(btn) == token)
            {
                iconText.Text = "\uE8C8";
                iconText.Foreground = TryFindResource("TextSecondaryBrush") as Brush ?? Brushes.Gray;
                btn.ToolTip = TranslationSource.Instance["compCopySha256Tooltip"];
            }
        }
    }

    private void ApplyNonAdminRestrictions()
    {
        BannerNonAdmin.Visibility = Visibility.Visible;

        string tooltipMsg = TranslationSource.Instance["compAdminRequiredTooltip"]
            ?? "Instalacja i zmiana wersji komponentów wymaga uruchomienia programu z uprawnieniami administratora.";

        BtnInstallLatest.IsEnabled = false;
        BtnInstallLatest.ToolTip = tooltipMsg;

        BtnInstallXampp.IsEnabled = false;
        BtnInstallXampp.ToolTip = tooltipMsg;

        BtnInstallPhp.IsEnabled = false;
        BtnInstallPhp.ToolTip = tooltipMsg;

        BtnInstallApache.IsEnabled = false;
        BtnInstallApache.ToolTip = tooltipMsg;

        BtnInstallMariaDb.IsEnabled = false;
        BtnInstallMariaDb.ToolTip = tooltipMsg;

        BtnInstallPma.IsEnabled = false;
        BtnInstallPma.ToolTip = tooltipMsg;

        CmbPhpVersions.IsEnabled = false;
        CmbApacheVersions.IsEnabled = false;
        CmbMariaDbVersions.IsEnabled = false;
        CmbPmaVersions.IsEnabled = false;
    }

    private async void BtnRestartAdmin_Click(object sender, RoutedEventArgs e)
    {
        BtnRestartAdmin.IsEnabled = false;
        var originalContent = BtnRestartAdmin.Content;
        BtnRestartAdmin.Content = "⏳ " + (TranslationSource.Instance["compStoppingServices"] ?? "Zatrzymywanie usług...");

        try
        {
            bool started = await AdminHelper.RestartAsAdministratorAsync(_serverService, "--manage-components");
            if (!started)
            {
                BtnRestartAdmin.IsEnabled = true;
                BtnRestartAdmin.Content = originalContent;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, TranslationSource.Instance["pampError"] ?? "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            BtnRestartAdmin.IsEnabled = true;
            BtnRestartAdmin.Content = originalContent;
        }
    }

    private void UpdateSinglePackageButton(ComboBox cmb, Button btn, string? installedVersion)
    {
        if (cmb.SelectedItem is not ComponentVersionInfo selectedVersion)
        {
            btn.IsEnabled = false;
            btn.ToolTip = null;
            return;
        }

        bool isAlreadyInstalled = !string.IsNullOrWhiteSpace(installedVersion) &&
                                  installedVersion != "—" &&
                                  string.Equals(NormalizeVersion(selectedVersion.Version), NormalizeVersion(installedVersion), StringComparison.OrdinalIgnoreCase);

        if (isAlreadyInstalled)
        {
            btn.IsEnabled = false;
            btn.ToolTip = TranslationSource.Instance["compAlreadyInstalled"];
        }
        else
        {
            btn.IsEnabled = true;
            btn.ToolTip = null;
        }
    }

    private void UpdateXamppButton(ComponentVersions? installed)
    {
        if (installed == null)
        {
            BtnInstallXampp.IsEnabled = true;
            BtnInstallXampp.ToolTip = null;
            return;
        }

        var xamppProfile = ComponentCatalog.GetXamppProfile();
        bool allXamppInstalled = xamppProfile.Count > 0 && xamppProfile.All(item =>
        {
            string? installedVer = item.Package.Id switch
            {
                "php" => installed.Php,
                "apache" => installed.Apache,
                "mariadb" => installed.MariaDb,
                "phpmyadmin" => installed.PhpMyAdmin,
                _ => null
            };

            return !string.IsNullOrWhiteSpace(installedVer) &&
                   installedVer != "—" &&
                   string.Equals(NormalizeVersion(item.Version.Version), NormalizeVersion(installedVer), StringComparison.OrdinalIgnoreCase);
        });

        if (allXamppInstalled)
        {
            BtnInstallXampp.IsEnabled = false;
            BtnInstallXampp.ToolTip = TranslationSource.Instance["xamppAlreadyInstalled"];
        }
        else
        {
            BtnInstallXampp.IsEnabled = true;
            BtnInstallXampp.ToolTip = null;
        }
    }

    private void UpdateLatestButton(ComponentVersions? installed)
    {
        if (installed == null)
        {
            BtnInstallLatest.IsEnabled = true;
            BtnInstallLatest.ToolTip = null;
            return;
        }

        var latestProfile = ComponentCatalog.GetLatestProfile();
        bool allLatestInstalled = latestProfile.Count > 0 && latestProfile.All(item =>
        {
            string? installedVer = item.Package.Id switch
            {
                "php" => installed.Php,
                "apache" => installed.Apache,
                "mariadb" => installed.MariaDb,
                "phpmyadmin" => installed.PhpMyAdmin,
                _ => null
            };

            return !string.IsNullOrWhiteSpace(installedVer) &&
                   installedVer != "—" &&
                   string.Equals(NormalizeVersion(item.Version.Version), NormalizeVersion(installedVer), StringComparison.OrdinalIgnoreCase);
        });

        if (allLatestInstalled)
        {
            BtnInstallLatest.IsEnabled = false;
            BtnInstallLatest.ToolTip = TranslationSource.Instance["compLatestAlreadyInstalled"];
        }
        else
        {
            BtnInstallLatest.IsEnabled = true;
            BtnInstallLatest.ToolTip = null;
        }
    }

    private static string NormalizeVersion(string? v) => string.IsNullOrWhiteSpace(v) ? "" : v.Trim().TrimStart('v', 'V');

    public async void BtnInstallXampp_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy) return;

        if (!AdminHelper.CanManageComponents)
        {
            MessageBox.Show(
                this,
                TranslationSource.Instance["compAdminRequiredDesc"] ?? "Instalacja oraz zmiana wersji komponentów wymaga uprawnień administratora.",
                TranslationSource.Instance["compAdminRequiredTitle"] ?? "Wymagane uprawnienia administratora",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            TranslationSource.Instance["xamppProfileConfirm"],
            TranslationSource.Instance["xamppProfileTitle"],
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        // Ostrzeżenie przed zmianą / downgrade wersji MariaDB przy istniejących danych
        var xamppMaria = ComponentCatalog.GetXamppProfile().FirstOrDefault(p => p.Package?.Id == "mariadb");
        if (xamppMaria.Package != null && xamppMaria.Version != null)
        {
            string dataDir = AppPaths.MariaDbDataDir;
            var currentManifest = ManifestLoader.Load();
            string? currentMaria = currentManifest.Versions?.MariaDb;
            if (Directory.Exists(dataDir) && !string.IsNullOrWhiteSpace(currentMaria) && currentMaria != "—")
            {
                if (!string.Equals(NormalizeVersion(xamppMaria.Version.Version), NormalizeVersion(currentMaria), StringComparison.OrdinalIgnoreCase))
                {
                    var warnRes = MessageBox.Show(
                        TranslationSource.Instance["compMariaDbDowngradeWarn"],
                        TranslationSource.Instance["pampWarning"],
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (warnRes != MessageBoxResult.Yes) return;
                }
            }
        }

        // Bezpieczne zatrzymanie usług przed aktualizacją binarek
        if (_serverService.IsApacheRunning || _serverService.IsMariaDbRunning)
        {
            await _serverService.StopAllAsync();
        }

        SetBusyState(true, BtnInstallXampp);
        PnlProfileProgress.Visibility = Visibility.Visible;

        var progress = new Progress<InstallProgress>(p =>
        {
            TxtProfileStatus.Text = p.StatusText;
            ProgProfile.IsIndeterminate = p.IsIndeterminate;
            if (!p.IsIndeterminate) ProgProfile.Value = p.Percentage;
        });

        try
        {
            await _installerService.InstallXamppProfileAsync(progress);

            LoadCurrentVersions();
            PopulateAvailableVersions();
            _onInstalledCallback?.Invoke();

            MessageBox.Show(
                TranslationSource.Instance["xamppProfileSuccess"],
                TranslationSource.Instance["pampSuccess"],
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                TranslationSource.Instance.Format("xamppProfileError", ex.Message),
                TranslationSource.Instance["pampWarning"],
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            SetBusyState(false, BtnInstallXampp);
            PnlProfileProgress.Visibility = Visibility.Collapsed;
        }
    }

    public async void BtnInstallLatest_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy) return;

        if (!AdminHelper.CanManageComponents)
        {
            MessageBox.Show(
                this,
                TranslationSource.Instance["compAdminRequiredDesc"] ?? "Instalacja oraz zmiana wersji komponentów wymaga uprawnień administratora.",
                TranslationSource.Instance["compAdminRequiredTitle"] ?? "Wymagane uprawnienia administratora",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            TranslationSource.Instance["compInstallLatestConfirm"],
            TranslationSource.Instance["compInstallLatestTitle"],
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        // Ostrzeżenie przed zmianą / downgrade wersji MariaDB przy istniejących danych
        var latestMaria = ComponentCatalog.GetLatestProfile().FirstOrDefault(p => p.Package?.Id == "mariadb");
        if (latestMaria.Package != null && latestMaria.Version != null)
        {
            string dataDir = AppPaths.MariaDbDataDir;
            var currentManifest = ManifestLoader.Load();
            string? currentMaria = currentManifest.Versions?.MariaDb;
            if (Directory.Exists(dataDir) && !string.IsNullOrWhiteSpace(currentMaria) && currentMaria != "—")
            {
                if (!string.Equals(NormalizeVersion(latestMaria.Version.Version), NormalizeVersion(currentMaria), StringComparison.OrdinalIgnoreCase))
                {
                    var warnRes = MessageBox.Show(
                        TranslationSource.Instance["compMariaDbDowngradeWarn"],
                        TranslationSource.Instance["pampWarning"],
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (warnRes != MessageBoxResult.Yes) return;
                }
            }
        }

        // Bezpieczne zatrzymanie usług przed aktualizacją binarek
        if (_serverService.IsApacheRunning || _serverService.IsMariaDbRunning)
        {
            await _serverService.StopAllAsync();
        }

        SetBusyState(true, BtnInstallLatest);
        PnlProfileProgress.Visibility = Visibility.Visible;

        var progress = new Progress<InstallProgress>(p =>
        {
            TxtProfileStatus.Text = p.StatusText;
            ProgProfile.IsIndeterminate = p.IsIndeterminate;
            if (!p.IsIndeterminate) ProgProfile.Value = p.Percentage;
        });

        try
        {
            await _installerService.InstallLatestProfileAsync(progress);

            LoadCurrentVersions();
            PopulateAvailableVersions();
            _onInstalledCallback?.Invoke();

            MessageBox.Show(
                TranslationSource.Instance["compInstallLatestSuccess"],
                TranslationSource.Instance["pampSuccess"],
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                TranslationSource.Instance.Format("compInstallLatestError", ex.Message),
                TranslationSource.Instance["pampWarning"],
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            SetBusyState(false, BtnInstallLatest);
            PnlProfileProgress.Visibility = Visibility.Collapsed;
        }
    }

    private async void BtnInstall_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy) return;
        if (sender is not Button { Tag: string packageId } btn) return;

        var package = ComponentCatalog.Packages.FirstOrDefault(p => p.Id == packageId);
        if (package is null) return;

        var (cmb, progBar, statusTxt, progressPnl, installedTxt) = packageId switch
        {
            "php" => (CmbPhpVersions, ProgPhp, TxtPhpStatus, PnlPhpProgress, TxtPhpInstalled),
            "apache" => (CmbApacheVersions, ProgApache, TxtApacheStatus, PnlApacheProgress, TxtApacheInstalled),
            "mariadb" => (CmbMariaDbVersions, ProgMariaDb, TxtMariaDbStatus, PnlMariaDbProgress, TxtMariaDbInstalled),
            "phpmyadmin" => (CmbPmaVersions, ProgPma, TxtPmaStatus, PnlPmaProgress, TxtPmaInstalled),
            _ => (null, null, null, null, null)
        };

        if (cmb?.SelectedItem is not ComponentVersionInfo selectedVersion) return;



        if (!AdminHelper.CanManageComponents)

        {

            MessageBox.Show(

                this,

                TranslationSource.Instance["compAdminRequiredDesc"] ?? "Instalacja oraz zmiana wersji komponentów wymaga uprawnień administratora.",

                TranslationSource.Instance["compAdminRequiredTitle"] ?? "Wymagane uprawnienia administratora",

                MessageBoxButton.OK,

                MessageBoxImage.Warning);

            return;

        }

        // Sprawdzenie czy powiązana usługa nie jest włączona
        if (packageId is "php" or "apache" && _serverService.IsApacheRunning)
        {
            var res = MessageBox.Show(
                TranslationSource.Instance.Get("compStopApacheWarn", "Apache jest aktualnie uruchomiony. Czy chcesz go zatrzymać, aby zaktualizować komponenty?"),
                TranslationSource.Instance["pampWarning"], MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                await _serverService.StopApacheAsync();
            }
            else return;
        }

        if (packageId is "mariadb")
        {
            if (_serverService.IsMariaDbRunning)
            {
                var res = MessageBox.Show(
                    TranslationSource.Instance.Get("compStopMariaDbWarn", "MariaDB jest aktualnie uruchomiona. Czy chcesz ją zatrzymać, aby zaktualizować komponenty?"),
                    TranslationSource.Instance["pampWarning"], MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                {
                    await _serverService.StopMariaDbAsync();
                }
                else return;
            }

            string dataDir = AppPaths.MariaDbDataDir;
            var currentManifest = ManifestLoader.Load();
            string? currentMaria = currentManifest.Versions?.MariaDb;
            if (Directory.Exists(dataDir) && !string.IsNullOrWhiteSpace(currentMaria) && currentMaria != "—")
            {
                if (!string.Equals(NormalizeVersion(selectedVersion.Version), NormalizeVersion(currentMaria), StringComparison.OrdinalIgnoreCase))
                {
                    var warnRes = MessageBox.Show(
                        TranslationSource.Instance["compMariaDbDowngradeWarn"],
                        TranslationSource.Instance["pampWarning"],
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (warnRes != MessageBoxResult.Yes) return;
                }
            }
        }

        SetBusyState(true, btn);
        if (progressPnl is not null) progressPnl.Visibility = Visibility.Visible;

        var progress = new Progress<InstallProgress>(p =>
        {
            if (statusTxt is not null) statusTxt.Text = p.StatusText;
            if (progBar is not null)
            {
                progBar.IsIndeterminate = p.IsIndeterminate;
                if (!p.IsIndeterminate) progBar.Value = p.Percentage;
            }
        });

        try
        {
            await _installerService.DownloadAndInstallAsync(package, selectedVersion, progress);

            LoadCurrentVersions();
            PopulateAvailableVersions();
            _onInstalledCallback?.Invoke();

            MessageBox.Show(
                $"{package.Name} {selectedVersion.Version}\n{TranslationSource.Instance["compInstallSuccess"]}",
                TranslationSource.Instance["pampSuccess"],
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                TranslationSource.Instance.Format("compInstallError", ex.Message),
                TranslationSource.Instance["pampWarning"],
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            SetBusyState(false, btn);
            if (progressPnl is not null) progressPnl.Visibility = Visibility.Collapsed;
        }
    }

    private void SetBusyState(bool busy, Button currentBtn)
    {
        _isBusy = busy;
        if (busy)
        {
            BtnInstallLatest.IsEnabled = false;
            BtnInstallXampp.IsEnabled = false;
            BtnInstallPhp.IsEnabled = false;
            BtnInstallApache.IsEnabled = false;
            BtnInstallMariaDb.IsEnabled = false;
            BtnInstallPma.IsEnabled = false;

            currentBtn.Content = TranslationSource.Instance["compInstalling"];
        }
        else
        {
            BtnInstallLatest.Content = TranslationSource.Instance["compInstallLatestBtn"];
            BtnInstallXampp.Content = TranslationSource.Instance["xamppProfileInstallBtn"];
            BtnInstallPhp.Content = TranslationSource.Instance["compInstallBtn"];
            BtnInstallApache.Content = TranslationSource.Instance["compInstallBtn"];
            BtnInstallMariaDb.Content = TranslationSource.Instance["compInstallBtn"];
            BtnInstallPma.Content = TranslationSource.Instance["compInstallBtn"];

            UpdateActionButtons();
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        if (Owner != null)
        {
            if (Owner.WindowState == WindowState.Minimized)
            {
                Owner.WindowState = WindowState.Normal;
            }
            Owner.Activate();
        }
    }
}
