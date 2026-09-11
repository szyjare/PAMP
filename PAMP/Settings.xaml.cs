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

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
