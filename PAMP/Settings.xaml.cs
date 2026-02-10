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
            this.Owner = owner;
            InitializeComponent();
            string currentLang = App.Settings.Language;
            if (currentLang == "pl")
            {
                RadioPL.IsChecked = true;
            }
            else
            {
                RadioEN.IsChecked = true;
            }

            _isInitialized = true;
        }

        private void Language_Checked(object sender, RoutedEventArgs e)
        {
            // Jeśli okno się dopiero tworzy, nie rób nic (chyba że chcesz przeładować język przy starcie jeszcze raz)
            if (!_isInitialized) return;

            var radioButton = sender as RadioButton;

            // Sprawdzamy czy przycisk jest faktycznie zaznaczony i czy ma Tag
            if (radioButton != null && radioButton.IsChecked == true && radioButton.Tag != null)
            {
                string langCode = radioButton.Tag.ToString(); // Pobierze "pl" lub "en"

                // A. Zmień język w aplikacji (natychmiastowo)
                TranslationSource.Instance.LoadLanguage(langCode);

                // B. Zapisz to w ustawieniach
                App.Settings.Language = langCode;
                App.Settings.Save();
            }
        }
    }
}
