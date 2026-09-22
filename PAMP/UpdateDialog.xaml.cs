using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace PAMP;

public partial class UpdateDialog : Window
{
    private readonly UpdatePackageInfo _update;
    private readonly ServerService _serverService;
    private CancellationTokenSource? _cts;
    private bool _isUpdating;

    public UpdateDialog(Window? owner, UpdatePackageInfo update, ServerService serverService)
    {
        Owner = owner;
        _update = update;
        _serverService = serverService;

        InitializeComponent();

        TxtCurrentVersion.Text = $"v{ManifestLoader.GetAppVersion()}";
        TxtNewVersion.Text = $"v{update.Version}";
        TxtChangelog.Text = !string.IsNullOrWhiteSpace(update.Changelog)
            ? update.Changelog
            : (TranslationSource.Instance["updateChangelog"] ?? "Dziennik zmian dla tej wersji nie został podany.");

        try
        {
            LinkGithubRelease.NavigateUri = new Uri(update.ReleaseUrl);
        }
        catch { }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        App.EnableMicaBackdrop(this);
        App.UpdateTitleBarTheme(this);
    }

    private async void BtnInstall_Click(object sender, RoutedEventArgs e)
    {
        if (_isUpdating) return;
        _isUpdating = true;

        BtnInstall.IsEnabled = false;
        BtnSkip.IsEnabled = false;
        BtnLater.IsEnabled = false;
        PanelProgress.Visibility = Visibility.Visible;

        _cts = new CancellationTokenSource();

        var progress = new Progress<InstallProgress>(p =>
        {
            Dispatcher.Invoke(() =>
            {
                TxtProgressStatus.Text = p.StatusText;
                ProgressBarUpdate.IsIndeterminate = p.IsIndeterminate;
                if (!p.IsIndeterminate)
                {
                    ProgressBarUpdate.Value = Math.Clamp(p.Percentage, 0, 100);
                }
            });
        });

        try
        {
            await UpdateService.Instance.DownloadAndApplyUpdateAsync(_update, _serverService, progress, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            _isUpdating = false;
            BtnInstall.IsEnabled = true;
            BtnSkip.IsEnabled = true;
            BtnLater.IsEnabled = true;
            PanelProgress.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            _isUpdating = false;
            BtnInstall.IsEnabled = true;
            BtnSkip.IsEnabled = true;
            BtnLater.IsEnabled = true;
            PanelProgress.Visibility = Visibility.Collapsed;

            string errorPrefix = TranslationSource.Instance["updateCheckError"] ?? "Błąd aktualizacji: ";
            MessageBox.Show(this, $"{errorPrefix}{ex.Message}", TranslationSource.Instance["pampWarning"] ?? "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnSkip_Click(object sender, RoutedEventArgs e)
    {
        if (_isUpdating) return;

        App.Settings.SkippedVersion = _update.Version;
        App.Settings.Save();
        Close();
    }

    private void BtnLater_Click(object sender, RoutedEventArgs e)
    {
        if (_isUpdating) return;
        Close();
    }

    private void LinkGithubRelease_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        catch { }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_isUpdating)
        {
            e.Cancel = true;
            return;
        }

        _cts?.Cancel();
        base.OnClosing(e);
    }
}
