using System.IO;
using System.Text.Json;

namespace PAMP
{
    public class AppSettings
    {
        private static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PAMP", "conf", "settings.json");

        public string Language { get; set; } = "en";
        public bool FirstRun { get; set; } = true;

        public static AppSettings Load()
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            try
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
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
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

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