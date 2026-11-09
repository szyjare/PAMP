using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows.Data;

namespace PAMP
{
    public class TranslationSource : INotifyPropertyChanged
    {
        private static readonly TranslationSource _instance = new TranslationSource();
        public static TranslationSource Instance => _instance;

        private Dictionary<string, string> _translations = new Dictionary<string, string>();

        public string this[string key]
        {
            get
            {
                if (_translations.TryGetValue(key, out string? value) && value is not null)
                    return value;
                return $"!{key}!";
            }
        }

        public string Get(string key, string? fallback = null)
        {
            if (_translations.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value))
                return value;
            return fallback ?? $"!{key}!";
        }

        public string Format(string key, params object[] args)
        {
            string pattern = Get(key);
            if (pattern.StartsWith("!") && pattern.EndsWith("!"))
            {
                return pattern;
            }
            try
            {
                return string.Format(pattern, args);
            }
            catch
            {
                return pattern;
            }
        }

        public void LoadLanguage(string langCode)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lang", $"{langCode}.json");

            if (!File.Exists(path))
            {
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lang", "en.json");
            }

            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];

                OnPropertyChanged("Item[]");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
