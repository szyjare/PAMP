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
    public partial class About : Window
    {
        public About(Window owner)
        {
            this.Owner = owner;
            InitializeComponent();
            var manifest = ManifestLoader.Load();
            about_version.Text = $"Wersja: {manifest.Versions.Pamp}";
        }

        public void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void Hyperlink_RequestNavigate(object sender, RoutedEventArgs e)
        {

        }
    }
}
