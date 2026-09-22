    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PAMP
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    /// 
    public partial class Settings : Window
    {
        private bool _isInitialized = false;

        public Settings(Window owner)
        {
            Owner = owner;
            InitializeComponent();

            string currentLang = App.Settings.Language;
            CmbLanguage.SelectedIndex = currentLang == "en" ? 1 : 0;
            ChkAutoCheckUpdates.IsChecked = App.Settings.AutoCheckUpdates;

            _isInitialized = true;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            App.EnableMicaBackdrop(this);
            App.UpdateTitleBarTheme(this);
        }

        private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (CmbLanguage.SelectedItem is ComboBoxItem { Tag: string langCode })
            {
                TranslationSource.Instance.LoadLanguage(langCode);
                App.Settings.Language = langCode;
                App.Settings.Save();
            }
        }

        private void ChkAutoCheckUpdates_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;

            App.Settings.AutoCheckUpdates = ChkAutoCheckUpdates.IsChecked == true;
            App.Settings.Save();
        }

        private async void BtnCheckUpdatesNow_Click(object sender, RoutedEventArgs e)
        {
            BtnCheckUpdatesNow.IsEnabled = false;
            string originalText = BtnCheckUpdatesNow.Content?.ToString() ?? "";
            BtnCheckUpdatesNow.Content = TranslationSource.Instance["updateChecking"] ?? "Sprawdzanie...";

            try
            {
                var update = await UpdateService.Instance.CheckForUpdatesAsync(force: true);
                if (update != null)
                {
                    var srv = (Owner as MainWindow)?.ServerService ?? new ServerService();
                    new UpdateDialog(this, update, srv).ShowDialog();
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
            finally
            {
                BtnCheckUpdatesNow.Content = originalText;
                BtnCheckUpdatesNow.IsEnabled = true;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
