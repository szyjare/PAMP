using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Navigation;

namespace PAMP;

public partial class ComponentManagerWindow : Window
{
    private readonly ServerService _serverService;
    private readonly ComponentInstallerService _installerService = new();
    private readonly Action? _onInstalledCallback;
    private bool _isBusy;

    public ComponentManagerWindow(Window owner, ServerService serverService, Action? onInstalledCallback = null)
    {
        Owner = owner;
        _serverService = serverService;
        _onInstalledCallback = onInstalledCallback;

        InitializeComponent();
        LoadCurrentVersions();
        PopulateAvailableVersions();

        Loaded += async (_, _) =>
        {
            TxtRepoStatus.Text = "repo.sjarecki.pl (synchronizacja...)";
            bool success = await ComponentCatalog.EnsureLoadedAsync();
            TxtRepoStatus.Text = success ? "✓ repo.sjarecki.pl" : "repo.sjarecki.pl (offline)";
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
        var manifest = ManifestLoader.Load();
        if (manifest.Versions is not { } v) return;

        TxtPhpInstalled.Text = v.Php;
        TxtApacheInstalled.Text = v.Apache;
        TxtMariaDbInstalled.Text = v.MariaDb;
        TxtPmaInstalled.Text = v.PhpMyAdmin;
    }

    private void PopulateAvailableVersions()
    {
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

            // 3. Fallback: profil CKE lub najnowsza wersja na górze
            toSelect ??= pkg.Versions.FirstOrDefault(v => v.IsCkeCompatible) ?? pkg.Versions.FirstOrDefault();
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

        var manifest = ManifestLoader.Load();
        var installed = manifest.Versions;

        UpdateSinglePackageButton(CmbPhpVersions, BtnInstallPhp, installed?.Php);
        UpdateSinglePackageButton(CmbApacheVersions, BtnInstallApache, installed?.Apache);
        UpdateSinglePackageButton(CmbMariaDbVersions, BtnInstallMariaDb, installed?.MariaDb);
        UpdateSinglePackageButton(CmbPmaVersions, BtnInstallPma, installed?.PhpMyAdmin);

        UpdateCkeButton(installed);
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

    private void UpdateCkeButton(ComponentVersions? installed)
    {
        if (installed == null)
        {
            BtnInstallCke.IsEnabled = true;
            BtnInstallCke.ToolTip = null;
            return;
        }

        var ckeProfile = ComponentCatalog.GetCkeProfile();
        bool allCkeInstalled = ckeProfile.Count > 0 && ckeProfile.All(item =>
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
                   string.Equals(NormalizeVersion(item.Version.Version), NormalizeVersion(installedVer), StringComparison.OrdinalIgnoreCase);
        });

        if (allCkeInstalled)
        {
            BtnInstallCke.IsEnabled = false;
            BtnInstallCke.ToolTip = TranslationSource.Instance["ckeAlreadyInstalled"];
        }
        else
        {
            BtnInstallCke.IsEnabled = true;
            BtnInstallCke.ToolTip = null;
        }
    }

    private static string NormalizeVersion(string? v) => string.IsNullOrWhiteSpace(v) ? "" : v.Trim().TrimStart('v', 'V');

    public async void BtnInstallCke_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy) return;

        var result = MessageBox.Show(
            TranslationSource.Instance["ckeProfileConfirm"],
            TranslationSource.Instance["ckeProfileTitle"],
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        // Bezpieczne zatrzymanie usług przed aktualizacją binarek
        if (_serverService.IsApacheRunning || _serverService.IsMariaDbRunning)
        {
            await _serverService.StopAllAsync();
        }

        SetBusyState(true, BtnInstallCke);
        PnlCkeProgress.Visibility = Visibility.Visible;

        var progress = new Progress<InstallProgress>(p =>
        {
            TxtCkeStatus.Text = p.StatusText;
            ProgCke.IsIndeterminate = p.IsIndeterminate;
            if (!p.IsIndeterminate) ProgCke.Value = p.Percentage;
        });

        try
        {
            await _installerService.InstallCkeProfileAsync(progress);

            LoadCurrentVersions();
            PopulateAvailableVersions();
            _onInstalledCallback?.Invoke();

            MessageBox.Show(
                TranslationSource.Instance["ckeProfileSuccess"],
                TranslationSource.Instance["pampSuccess"],
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Błąd instalacji profilu CKE: {ex.Message}",
                TranslationSource.Instance["pampWarning"],
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            SetBusyState(false, BtnInstallCke);
            PnlCkeProgress.Visibility = Visibility.Collapsed;
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

        // Sprawdzenie czy powiązana usługa nie jest włączona
        if (packageId is "php" or "apache" && _serverService.IsApacheRunning)
        {
            var res = MessageBox.Show(
                "Apache jest aktualnie uruchomiony. Czy chcesz go zatrzymać, aby zaktualizować komponenty?",
                TranslationSource.Instance["pampWarning"], MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                await _serverService.StopApacheAsync();
            }
            else return;
        }

        if (packageId is "mariadb" && _serverService.IsMariaDbRunning)
        {
            var res = MessageBox.Show(
                "MariaDB jest aktualnie uruchomiona. Czy chcesz ją zatrzymać, aby zaktualizować komponenty?",
                TranslationSource.Instance["pampWarning"], MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                await _serverService.StopMariaDbAsync();
            }
            else return;
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
                $"Błąd instalacji: {ex.Message}",
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
            BtnInstallCke.IsEnabled = false;
            BtnInstallPhp.IsEnabled = false;
            BtnInstallApache.IsEnabled = false;
            BtnInstallMariaDb.IsEnabled = false;
            BtnInstallPma.IsEnabled = false;

            currentBtn.Content = TranslationSource.Instance["compInstalling"];
        }
        else
        {
            BtnInstallCke.Content = TranslationSource.Instance["ckeProfileInstallBtn"];
            BtnInstallPhp.Content = TranslationSource.Instance["compInstallBtn"];
            BtnInstallApache.Content = TranslationSource.Instance["compInstallBtn"];
            BtnInstallMariaDb.Content = TranslationSource.Instance["compInstallBtn"];
            BtnInstallPma.Content = TranslationSource.Instance["compInstallBtn"];

            UpdateActionButtons();
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}
