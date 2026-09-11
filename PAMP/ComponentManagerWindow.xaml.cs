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

            if (cmb is not null)
            {
                cmb.ItemsSource = pkg.Versions;
                var recommended = pkg.Versions.FirstOrDefault(v => v.IsCkeCompatible) ?? pkg.Versions.FirstOrDefault();
                cmb.SelectedItem = recommended;
            }
        }
    }

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

            if (installedTxt is not null) installedTxt.Text = selectedVersion.Version;
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
        BtnInstallCke.IsEnabled = !busy;
        BtnInstallPhp.IsEnabled = !busy;
        BtnInstallApache.IsEnabled = !busy;
        BtnInstallMariaDb.IsEnabled = !busy;
        BtnInstallPma.IsEnabled = !busy;
        if (currentBtn == BtnInstallCke)
        {
            currentBtn.Content = busy ? TranslationSource.Instance["compInstalling"] : TranslationSource.Instance["ckeProfileInstallBtn"];
        }
        else
        {
            currentBtn.Content = busy ? TranslationSource.Instance["compInstalling"] : TranslationSource.Instance["compInstallBtn"];
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}
