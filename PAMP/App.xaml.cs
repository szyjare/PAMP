using System.Configuration;
using System.Data;
using System.Windows;

namespace PAMP
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static AppSettings Settings { get; private set; }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Settings = AppSettings.Load();
            TranslationSource.Instance.LoadLanguage(Settings.Language);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Settings.Save();
            base.OnExit(e);
        }
    }

}
