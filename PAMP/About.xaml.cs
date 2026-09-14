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
using System.Windows.Navigation;

namespace PAMP
{
    public partial class About : Window
    {
        public About(Window owner)
        {
            this.Owner = owner;
            InitializeComponent();
            var manifest = ManifestLoader.Load();
            about_version.Text = $"{TranslationSource.Instance["version"]} {manifest.Versions.Pamp}";
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            App.EnableMicaBackdrop(this);
            App.UpdateTitleBarTheme(this);
        }

        public void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch { }
        }
    }
}
