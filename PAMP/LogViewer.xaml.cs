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
        private EnvironmentManager _envManager;
        private DispatcherTimer _timer;
        private string _currentFilePath;
        private long _lastFileSize = 0;

        private Dictionary<string, string> _logFiles = new Dictionary<string, string>();

        public LogViewer()
        {
            InitializeComponent();
            _envManager = new EnvironmentManager();
            InitializeLogs();
            SetupTimer();
        }

        private void InitializeLogs()
        {
            // Pobieramy ścieżki z EnvironmentManagera
            string appDataPamp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PAMP");
            string logsDir = Path.Combine(appDataPamp, "logs");

            _logFiles.Add("Apache: System i Błędy", Path.Combine(logsDir, "apache_error.log"));
            _logFiles.Add("Apache: Ruch sieciowy", Path.Combine(logsDir, "apache_access.log"));
            _logFiles.Add("MariaDB: System", Path.Combine(logsDir, "mysql_error.log"));

            foreach (var name in _logFiles.Keys)
            {
                ComboLogFiles.Items.Add(name);
            }
            ComboLogFiles.SelectedIndex = 0;
        }

        private void SetupTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(2); // Odświeżaj co 2 sekundy
            _timer.Tick += (s, e) => ReadLogUpdate();
            _timer.Start();
        }

        private void ComboLogFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedName = ComboLogFiles.SelectedItem as string;
            if (selectedName != null && _logFiles.ContainsKey(selectedName))
            {
                _currentFilePath = _logFiles[selectedName];
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
        private void ReadLogUpdate(bool forceFullReload = false)
        {
            if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
            {
                if (forceFullReload)
                {
                    LogParagraph.Inlines.Clear();
                    LogParagraph.Inlines.Add(new Run("Plik logu jeszcze nie istnieje.") { Foreground = Brushes.Gray });
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

                        foreach (var line in lines)
                        {
                            if (string.IsNullOrEmpty(line)) continue;

                            // Tworzy element tekstu
                            Run run = new Run(line + Environment.NewLine);

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
                                run.Foreground = Brushes.Black;
                            }
                            // --------------------------

                            LogParagraph.Inlines.Add(run);
                            LogParagraph.Inlines.Add(new Run("\n") { Foreground = Brushes.Gray });
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