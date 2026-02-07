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
                if (_translations.TryGetValue(key, out string value))
                    return value;
                return $"!{key}!";
            }
        }

        public void LoadLanguage(string langCode)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lang", $"{langCode}.json");

            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                OnPropertyChanged("Item[]");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}