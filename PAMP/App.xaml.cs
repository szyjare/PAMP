using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace PAMP;

public partial class App : Application
{
    public static AppSettings Settings { get; private set; } = null!;

    // Zamrożone pędzle - zero alokacji GC przy przełączaniu motywu
    private static readonly SolidColorBrush DarkCardBackground = CreateFrozenBrush(Color.FromArgb(160, 32, 32, 32));
    private static readonly SolidColorBrush DarkCardBorder = CreateFrozenBrush(Color.FromArgb(35, 255, 255, 255));
    private static readonly SolidColorBrush LightCardBackground = CreateFrozenBrush(Color.FromArgb(200, 255, 255, 255));
    private static readonly SolidColorBrush LightCardBorder = CreateFrozenBrush(Color.FromArgb(25, 0, 0, 0));

    // Pędzle dla Windows 10 / środowisk bez obsługi Mica
    private static readonly SolidColorBrush TransparentBrush = CreateFrozenBrush(Colors.Transparent);
    private static readonly SolidColorBrush LightWindowBackground = CreateFrozenBrush(Color.FromRgb(243, 243, 243));
    private static readonly SolidColorBrush DarkWindowBackground = CreateFrozenBrush(Color.FromRgb(32, 32, 32));
    private static readonly SolidColorBrush LightCardBackgroundSolid = CreateFrozenBrush(Color.FromRgb(255, 255, 255));
    private static readonly SolidColorBrush DarkCardBackgroundSolid = CreateFrozenBrush(Color.FromRgb(43, 43, 43));

    private static readonly SolidColorBrush DarkTextPrimary = CreateFrozenBrush(Color.FromRgb(245, 245, 245));
    private static readonly SolidColorBrush DarkTextSecondary = CreateFrozenBrush(Color.FromArgb(180, 255, 255, 255));
    private static readonly SolidColorBrush LightTextPrimary = CreateFrozenBrush(Color.FromRgb(26, 26, 26));
    private static readonly SolidColorBrush LightTextSecondary = CreateFrozenBrush(Color.FromArgb(160, 0, 0, 0));

    public static bool SupportsMica => Environment.OSVersion.Version.Build >= 22000;

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

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
            Dispatcher.Invoke(() =>
            {
                ApplyThemeResources();
                foreach (Window window in Windows)
                {
                    UpdateTitleBarTheme(window);
                }
            });
        }
    }

    public static void ApplyThemeResources()
    {
        if (Current == null) return;

        bool isDark = IsDarkTheme();

        var textPrimary = isDark ? DarkTextPrimary : LightTextPrimary;
        var textSecondary = isDark ? DarkTextSecondary : LightTextSecondary;

        bool mica = SupportsMica;
        Current.Resources["WindowBackgroundBrush"] = mica
            ? TransparentBrush
            : (isDark ? DarkWindowBackground : LightWindowBackground);

        Current.Resources["CardBackgroundBrush"] = mica
            ? (isDark ? DarkCardBackground : LightCardBackground)
            : (isDark ? DarkCardBackgroundSolid : LightCardBackgroundSolid);

        Current.Resources["CardBorderBrush"] = isDark ? DarkCardBorder : LightCardBorder;
        Current.Resources["TextPrimaryBrush"] = textPrimary;
        Current.Resources["TextSecondaryBrush"] = textSecondary;

        // Dynamiczne pędzle systemowego koloru akcentu Windows (Windows 10/11 Accent Color)
        Color accent = GetWindowsAccentColor();
        var accentBrush = CreateFrozenBrush(accent);
        byte bgAlpha = (byte)(isDark ? 36 : 24);
        var accentBackgroundBrush = CreateFrozenBrush(Color.FromArgb(bgAlpha, accent.R, accent.G, accent.B));
        var accentBorderBrush = CreateFrozenBrush(Color.FromArgb(100, accent.R, accent.G, accent.B));
        double luminance = 0.299 * accent.R + 0.587 * accent.G + 0.114 * accent.B;
        var accentForegroundBrush = luminance > 165 ? CreateFrozenBrush(Color.FromRgb(20, 20, 20)) : CreateFrozenBrush(Colors.White);

        Current.Resources["AccentBrush"] = accentBrush;
        Current.Resources["AccentBackgroundBrush"] = accentBackgroundBrush;
        Current.Resources["AccentBorderBrush"] = accentBorderBrush;
        Current.Resources["AccentForegroundBrush"] = accentForegroundBrush;

        // Nadpisujemy klucze systemowe, aby standardowe kontrolki WPF i teksty dziedziczące miały pełny kontrast w Dark Mode
        Current.Resources[SystemColors.WindowTextBrushKey] = textPrimary;
        Current.Resources[SystemColors.GrayTextBrushKey] = textSecondary;
        Current.Resources[SystemColors.MenuTextBrushKey] = textPrimary;
    }

    public static Color GetWindowsAccentColor()
    {
        try
        {
            // Windows 10/11 Explorer AccentPalette (Index 3 to właściwy kolor akcentu)
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent");
            if (key?.GetValue("AccentPalette") is byte[] palette && palette.Length >= 16)
            {
                return Color.FromRgb(palette[12], palette[13], palette[14]);
            }
        }
        catch { }

        try
        {
            // Fallback 1: DWM AccentColor (format 0xAABBGGRR)
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
            if (key?.GetValue("AccentColor") is int rawAccent)
            {
                byte r = (byte)(rawAccent & 0xFF);
                byte g = (byte)((rawAccent >> 8) & 0xFF);
                byte b = (byte)((rawAccent >> 16) & 0xFF);
                return Color.FromRgb(r, g, b);
            }
        }
        catch { }

        // Fallback 2: domyślny systemowy błękit (#0078D4)
        return Color.FromRgb(0, 120, 212);
    }

    public static void EnableMicaBackdrop(Window window)
    {
        if (!SupportsMica)
        {
            // Windows 10 lub starszy: brak Mica, wymuszamy solidne tło okna
            window.Background = (Brush)Current.Resources["WindowBackgroundBrush"];
            return;
        }

        nint handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        if (handle == nint.Zero) return;

        int backdropType = NativeMethods.DWMSBT_MAINWINDOW;
        int hr = NativeMethods.DwmSetWindowAttribute(
            handle,
            NativeMethods.DWMWA_SYSTEMBACKDROP_TYPE,
            ref backdropType,
            sizeof(int));

        if (hr != 0)
        {
            // Jeśli system zgłosił błąd przy włączaniu Mica, wycofujemy się do jednolitego tła
            bool isDark = IsDarkTheme();
            window.Background = isDark ? DarkWindowBackground : LightWindowBackground;
        }
    }

    public static void UpdateTitleBarTheme(Window window)
    {
        nint handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        if (handle == nint.Zero) return;

        int isDark = IsDarkTheme() ? 1 : 0;
        _ = NativeMethods.DwmSetWindowAttribute(
            handle,
            NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref isDark,
            sizeof(int));
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
