using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Documents; 
using System.Windows.Media;     

namespace PAMP
{
    public partial class LogViewer : Window
    {
        private readonly EnvironmentManager _envManager = new();
        private readonly DispatcherTimer _timer = new();
        private string _currentFilePath = string.Empty;
        private long _lastFileSize = 0;

        private readonly Dictionary<string, string> _logFiles = [];

        public LogViewer()
        {
            InitializeComponent();
            InitializeLogs();
            SetupTimer();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            App.EnableMicaBackdrop(this);
            App.UpdateTitleBarTheme(this);
        }

        private void InitializeLogs()
        {
            // Pobieramy ścieżki z EnvironmentManagera
            string appDataPamp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PAMP");
            string logsDir = Path.Combine(appDataPamp, "logs");

            _logFiles.Add(TranslationSource.Instance["logsListApacheLogError"], Path.Combine(logsDir, "apache_error.log"));
            _logFiles.Add(TranslationSource.Instance["logsListApacheNetwork"], Path.Combine(logsDir, "apache_access.log"));
            _logFiles.Add(TranslationSource.Instance["logsListDbSystem"], Path.Combine(logsDir, "mysql_error.log"));

            foreach (var name in _logFiles.Keys)
            {
                ComboLogFiles.Items.Add(name);
            }
            ComboLogFiles.SelectedIndex = 0;
        }

        private void SetupTimer()
        {
            _timer.Interval = TimeSpan.FromSeconds(2); // Odświeżaj co 2 sekundy
            _timer.Tick += (s, e) => ReadLogUpdate();
            _timer.Start();
        }

        private void ComboLogFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboLogFiles.SelectedItem is string selectedName && _logFiles.TryGetValue(selectedName, out var filePath))
            {
                _currentFilePath = filePath;
                _lastFileSize = 0;

                // Czyszczenie RichTextBoxa
                LogParagraph.Inlines.Clear();

                ReadLogUpdate(forceFullReload: true);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            ReadLogUpdate();
        }

        // Główna funkcja czytająca
        private static readonly FontFamily ConsoleFont = new("Consolas, Cascadia Mono, Lucida Console, monospace");

        private void ReadLogUpdate(bool forceFullReload = false)
        {
            if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
            {
                if (forceFullReload)
                {
                    LogParagraph.Inlines.Clear();
                    LogParagraph.Inlines.Add(new Run(TranslationSource.Instance["logsLogFileDoesntExist"]) 
                    { 
                        FontFamily = ConsoleFont,
                        FontSize = 12.5,
                        Foreground = (Brush)Application.Current.FindResource("TextSecondaryBrush") 
                    });
                }
                return;
            }

            try
            {
                using (FileStream fs = new FileStream(_currentFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long currentLength = fs.Length;
                    if (currentLength < _lastFileSize) _lastFileSize = 0;
                    if (currentLength == _lastFileSize) return;

                    fs.Seek(_lastFileSize, SeekOrigin.Begin);

                    using (StreamReader sr = new StreamReader(fs, Encoding.Default))
                    {
                        string newContent = sr.ReadToEnd();

                        // Dzieli na linie, żeby każdą pokolorować
                        string[] lines = newContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                        var defaultBrush = (Brush)Application.Current.FindResource("TextPrimaryBrush");

                        foreach (var line in lines)
                        {
                            if (string.IsNullOrEmpty(line)) continue;

                            // Tworzy element tekstu ze ścisłą czcionką konsolową
                            Run run = new Run(line + Environment.NewLine)
                            {
                                FontFamily = ConsoleFont,
                                FontSize = 12.5
                            };

                            // --- LOGIKA KOLOROWANIA ---
                            string lowerLine = line.ToLower();

                            if (lowerLine.Contains("[error]") || lowerLine.Contains("[crit]") || lowerLine.Contains("[emerg]") || lowerLine.Contains(" fatal "))
                            {
                                run.Foreground = Brushes.Red;
                                run.FontWeight = FontWeights.Bold;
                            }
                            else if (lowerLine.Contains("[warn]") || lowerLine.Contains("[warning]"))
                            {
                                run.Foreground = Brushes.DarkOrange;
                            }
                            else if (lowerLine.Contains("[notice]") || lowerLine.Contains("[info]") || lowerLine.Contains("[note]"))
                            {
                                run.Foreground = Brushes.Green;
                            }
                            else
                            {
                                run.Foreground = defaultBrush;
                            }
                            // --------------------------

                            LogParagraph.Inlines.Add(run);
                        }

                        if (ChkAutoScroll.IsChecked == true)
                        {
                            RtbLogContent.Dispatcher.InvokeAsync(() =>
                            {
                                RtbLogContent.ScrollToEnd();
                            }, System.Windows.Threading.DispatcherPriority.Background);
                        }
                    }
                    _lastFileSize = currentLength;
                }
            }
            catch { }
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            base.OnClosed(e);
        }
    }
}