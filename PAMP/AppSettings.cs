using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PAMP
{
    public class AppSettings
    {
        private static string SettingsPath => Path.Combine(AppPaths.ConfDir, "settings.json");

        public string Language { get; set; } = DetectDefaultLanguage();
        public bool FirstRun { get; set; } = true;
        public bool AutoCheckUpdates { get; set; } = true;
        public string? SkippedVersion { get; set; } = null;
        public DateTime? LastUpdateCheckUtc { get; set; } = null;
        public bool MinimizeToTray { get; set; } = true;
        public bool CloseToTray { get; set; } = true;
        public bool ShowTrayCloseNotification { get; set; } = true;

        private bool? _includeBetaUpdates;

        [JsonPropertyName("includeBetaUpdates")]
        public bool? IncludeBetaUpdatesRaw
        {
            get => _includeBetaUpdates;
            set => _includeBetaUpdates = value;
        }

        [JsonIgnore]
        public bool IncludeBetaUpdates
        {
            get => _includeBetaUpdates ?? DetectDefaultIncludeBeta();
            set => _includeBetaUpdates = value;
        }

        public static bool DetectDefaultIncludeBeta()
        {
            string ver = ManifestLoader.GetAppVersion();
            return ver.Contains("beta", StringComparison.OrdinalIgnoreCase) ||
                   ver.Contains("alpha", StringComparison.OrdinalIgnoreCase) ||
                   ver.Contains("rc", StringComparison.OrdinalIgnoreCase) ||
                   ver.Contains("preview", StringComparison.OrdinalIgnoreCase);
        }

        public static string DetectDefaultLanguage()
        {
            try
            {
                var sysLang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
                var langPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lang", $"{sysLang}.json");
                if (File.Exists(langPath))
                {
                    return sysLang;
                }
            }
            catch
            {
                // Fallback do języka angielskiego w razie błędu
            }

            return "en";
        }

        public static AppSettings Load()
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            try
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                if (string.IsNullOrWhiteSpace(settings.Language))
                {
                    settings.Language = DetectDefaultLanguage();
                }
                return settings;
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd zapisu ustawień: {ex.Message}");
            }
        }
    }
}
