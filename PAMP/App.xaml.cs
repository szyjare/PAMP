using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace PAMP;

public partial class App : Application
{
    public static AppSettings Settings { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ApplyThemeResources();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        Settings = AppSettings.Load();
        TranslationSource.Instance.LoadLanguage(Settings.Language);
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color)
        {
            Dispatcher.Invoke(ApplyThemeResources);
        }
    }

    public static void ApplyThemeResources()
    {
        bool isDark = IsDarkTheme();

        if (isDark)
        {
            // Półprzezroczyste ciemne karty na tle Mica w trybie ciemnym (styl Windows 11 Fluent)
            Current.Resources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(175, 33, 33, 33));
            Current.Resources["CardBorderBrush"] = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255));
            Current.Resources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            Current.Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255));
        }
        else
        {
            // Półprzezroczyste jasne karty na tle Mica w trybie jasnym
            Current.Resources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(215, 255, 255, 255));
            Current.Resources["CardBorderBrush"] = new SolidColorBrush(Color.FromArgb(28, 0, 0, 0));
            Current.Resources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            Current.Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0));
        }
    }

    public static bool IsDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i == 0;
        }
        catch
        {
            return false;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        Settings?.Save();
        base.OnExit(e);
    }
}
