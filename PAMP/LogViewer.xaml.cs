using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace PAMP;

public partial class LogViewer : Window
{
    public enum LogLevel { Info, Warning, Error }

    private class LogEntry
    {
        public required string RawText { get; init; }
        public required LogLevel Level { get; init; }
        public required Paragraph Paragraph { get; init; }
    }

    private readonly DispatcherTimer _timer = new();
    private readonly DispatcherTimer _searchDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(180) };
    private string _currentFilePath = string.Empty;
    private long _lastFileSize = 0;
    private readonly Dictionary<string, string> _logFiles = [];
    private readonly List<LogEntry> _allEntries = [];
    private string _currentFilterLevel = "all";
    private string _currentSearchQuery = string.Empty;
    private bool _isInitialized = false;

    private const int MaxInitialBytes = 150_000;
    private const int MaxRetainedEntries = 1500;

    // Konsolowa czcionka o stałej szerokości
    private static readonly FontFamily ConsoleFont = new("Consolas, Cascadia Mono, Lucida Console, monospace");

    // Zamrożone pędzle zapewniające maksymalną wydajność i doskonały kontrast (WCAG)
    private static readonly Brush TimestampBrush = CreateFrozenBrush(Color.FromRgb(148, 163, 184)); // Slate 400
    private static readonly Brush MetadataBrush = CreateFrozenBrush(Color.FromRgb(100, 116, 139));  // Slate 500
    private static readonly Brush PrimaryTextBrush = CreateFrozenBrush(Color.FromRgb(226, 232, 240)); // Slate 200 (czytelny off-white)
    private static readonly Brush ErrorTextBrush = CreateFrozenBrush(Color.FromRgb(248, 113, 113));   // Red 400
    private static readonly Brush ErrorBadgeBrush = CreateFrozenBrush(Color.FromRgb(239, 68, 68));    // Red 500
    private static readonly Brush WarnTextBrush = CreateFrozenBrush(Color.FromRgb(251, 191, 36));     // Amber 400
    private static readonly Brush WarnBadgeBrush = CreateFrozenBrush(Color.FromRgb(245, 158, 11));    // Amber 500
    private static readonly Brush NoteBadgeBrush = CreateFrozenBrush(Color.FromRgb(56, 189, 248));    // Sky 400 (błękitny zamiast ciemnozielonego!)
    private static readonly Brush NoticeBadgeBrush = CreateFrozenBrush(Color.FromRgb(125, 211, 252)); // Sky 300
    private static readonly Brush SuccessBrush = CreateFrozenBrush(Color.FromRgb(74, 222, 128));      // Green 400
    private static readonly Brush MethodGetBrush = CreateFrozenBrush(Color.FromRgb(56, 189, 248));    // Sky 400
    private static readonly Brush MethodPostBrush = CreateFrozenBrush(Color.FromRgb(74, 222, 128));   // Green 400
    private static readonly Brush MethodPutBrush = CreateFrozenBrush(Color.FromRgb(251, 191, 36));    // Amber 400
    private static readonly Brush MethodDeleteBrush = CreateFrozenBrush(Color.FromRgb(248, 113, 113)); // Red 400
    private static readonly Brush MethodOtherBrush = CreateFrozenBrush(Color.FromRgb(192, 132, 252)); // Purple 400

    private static readonly Brush ErrorStripeBrush = CreateFrozenBrush(Color.FromRgb(239, 68, 68));
    private static readonly Brush WarnStripeBrush = CreateFrozenBrush(Color.FromRgb(245, 158, 11));
    private static readonly Brush TransparentBrush = Brushes.Transparent;

    // Wyrażenia regularne do semantycznego parsowania formatów logów
    private static readonly Regex MariaDbRegex = new(@"^(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2})\s+(\d+)\s+\[([^\]]+)\]\s*(.*)$", RegexOptions.Compiled);
    private static readonly Regex ApacheErrorRegex = new(@"^\[([^\]]+)\]\s+\[([^\]]+)\](?:\s+\[([^\]]+)\])?\s*(.*)$", RegexOptions.Compiled);
    private static readonly Regex ApacheAccessRegex = new(@"^(\S+)\s+\S+\s+\S+\s+\[([^\]]+)\]\s+""([A-Z]+)\s+([^\s""]+)(?:\s+([^""]+))?""\s+(\d{3})\s+(\S+)(?:\s+""([^""]*)""\s+""([^""]*)"")?", RegexOptions.Compiled);

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var b = new SolidColorBrush(color);
        b.Freeze();
        return b;
    }

    public LogViewer()
    {
        InitializeComponent();
        _isInitialized = true;
        ComboLogLevelFilter.SelectedIndex = 0;
        InitializeLogs();
        SetupTimer();
        SetupSearchDebounce();
    }

    private void SetupSearchDebounce()
    {
        _searchDebounceTimer.Tick += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            ApplyFilterToDocument();
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        App.EnableMicaBackdrop(this);
        App.UpdateTitleBarTheme(this);
    }

    private void InitializeLogs()
    {
        string appDataPamp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PAMP");
        string logsDir = Path.Combine(appDataPamp, "logs");

        _logFiles.Add(TranslationSource.Instance["logsListApacheLogError"] ?? "Błędy i system Apache", Path.Combine(logsDir, "apache_error.log"));
        _logFiles.Add(TranslationSource.Instance["logsListApacheNetwork"] ?? "Dostęp Apache (HTTP)", Path.Combine(logsDir, "apache_access.log"));
        _logFiles.Add(TranslationSource.Instance["logsListDbSystem"] ?? "System MariaDB", Path.Combine(logsDir, "mysql_error.log"));

        foreach (var name in _logFiles.Keys)
        {
            ComboLogFiles.Items.Add(name);
        }
        ComboLogFiles.SelectedIndex = 0;
    }

    private void SetupTimer()
    {
        _timer.Interval = TimeSpan.FromSeconds(2);
        _timer.Tick += (_, _) => ReadLogUpdate();
        _timer.Start();
    }

    private void ComboLogFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized) return;
        if (ComboLogFiles.SelectedItem is string selectedName && _logFiles.TryGetValue(selectedName, out var filePath))
        {
            _currentFilePath = filePath;
            _lastFileSize = 0;
            _allEntries.Clear();
            if (DocLogs != null)
                DocLogs.Blocks.Clear();
            if (TxtLogStatus != null)
                TxtLogStatus.Text = filePath;

            ReadLogUpdate(forceFullReload: true);
        }
    }

    private void ComboLogLevelFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized) return;
        if (ComboLogLevelFilter.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            _currentFilterLevel = tag;
            ApplyFilterToDocument();
        }
    }

    private void TxtSearchFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized) return;
        _currentSearchQuery = TxtSearchFilter.Text.Trim();
        TxtSearchPlaceholder.Visibility = string.IsNullOrEmpty(_currentSearchQuery) ? Visibility.Visible : Visibility.Collapsed;
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        ReadLogUpdate();
    }

    private void BtnClearView_Click(object sender, RoutedEventArgs e)
    {
        _allEntries.Clear();
        if (DocLogs != null)
            DocLogs.Blocks.Clear();
        if (TxtLineCount != null)
            TxtLineCount.Text = "0 wpisów";
    }

    private void BtnOpenInEditor_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
        {
            try
            {
                Process.Start(new ProcessStartInfo(_currentFilePath) { UseShellExecute = true });
            }
            catch
            {
                Process.Start(new ProcessStartInfo("notepad.exe", _currentFilePath) { UseShellExecute = true });
            }
        }
    }

    private void ReadLogUpdate(bool forceFullReload = false)
    {
        if (!_isInitialized || string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
        {
            if (forceFullReload && DocLogs != null)
            {
                _allEntries.Clear();
                DocLogs.Blocks.Clear();

                var emptyParagraph = new Paragraph
                {
                    Margin = new Thickness(0, 10, 0, 10),
                    Padding = new Thickness(6),
                    FontFamily = ConsoleFont,
                    FontSize = 12.5
                };
                emptyParagraph.Inlines.Add(new Run(TranslationSource.Instance["logsLogFileDoesntExist"] ?? "Plik logu jeszcze nie istnieje.")
                {
                    Foreground = (Brush)Application.Current.FindResource("TextSecondaryBrush")
                });
                DocLogs.Blocks.Add(emptyParagraph);
                if (TxtLineCount != null)
                    TxtLineCount.Text = "0 wpisów";
            }
            return;
        }

        try
        {
            using var fs = new FileStream(_currentFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            long currentLength = fs.Length;
            if (currentLength < _lastFileSize) _lastFileSize = 0;
            if (currentLength == _lastFileSize && !forceFullReload) return;

            if (forceFullReload)
            {
                _allEntries.Clear();
                if (currentLength > MaxInitialBytes)
                {
                    fs.Seek(currentLength - MaxInitialBytes, SeekOrigin.Begin);
                    // Pomiń pierwszą uciętą linię czytając bajty do znaku nowej linii
                    int b;
                    while ((b = fs.ReadByte()) != -1 && b != '\n') { }
                }
                else
                {
                    fs.Seek(0, SeekOrigin.Begin);
                }
            }
            else
            {
                fs.Seek(_lastFileSize, SeekOrigin.Begin);
            }

            using var sr = new StreamReader(fs, Encoding.Default);
            string? line;
            bool isAccessLog = _currentFilePath.EndsWith("apache_access.log", StringComparison.OrdinalIgnoreCase);
            bool isApacheError = _currentFilePath.EndsWith("apache_error.log", StringComparison.OrdinalIgnoreCase);
            bool isMariaDb = _currentFilePath.EndsWith("mysql_error.log", StringComparison.OrdinalIgnoreCase);

            var newEntries = new List<LogEntry>();

            while ((line = sr.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                LogEntry entry = ParseLine(line, isAccessLog, isApacheError, isMariaDb);
                newEntries.Add(entry);
            }

            _lastFileSize = currentLength;

            if (newEntries.Count == 0 && !forceFullReload) return;

            _allEntries.AddRange(newEntries);

            // Bounded buffer: nie pozwalamy na nieskończony wzrost pamięci przy gigantycznych logach
            if (_allEntries.Count > MaxRetainedEntries)
            {
                int removeCount = _allEntries.Count - MaxRetainedEntries;
                _allEntries.RemoveRange(0, removeCount);
                ApplyFilterToDocument();
                return;
            }

            if (forceFullReload)
            {
                ApplyFilterToDocument();
            }
            else
            {
                bool anyAdded = false;
                DocLogs.BeginInit();
                try
                {
                    foreach (var entry in newEntries)
                    {
                        if (MatchesFilter(entry))
                        {
                            DocLogs.Blocks.Add(entry.Paragraph);
                            anyAdded = true;
                        }
                    }
                }
                finally
                {
                    DocLogs.EndInit();
                }

                UpdateLineCountText();

                if (anyAdded && ChkAutoScroll.IsChecked == true)
                {
                    RtbLogContent.Dispatcher.InvokeAsync(() =>
                    {
                        RtbLogContent.ScrollToEnd();
                    }, DispatcherPriority.Background);
                }
            }
        }
        catch { }
    }

    private LogEntry ParseLine(string line, bool isAccessLog, bool isApacheError, bool isMariaDb)
    {
        var p = new Paragraph
        {
            Margin = new Thickness(0, 1.5, 0, 1.5),
            LineHeight = 19,
            FontFamily = ConsoleFont,
            FontSize = 12
        };

        LogLevel level = LogLevel.Info;
        Brush stripeBrush = TransparentBrush;

        if (isMariaDb)
        {
            var match = MariaDbRegex.Match(line);
            if (match.Success)
            {
                string timeStr = match.Groups[1].Value;
                string threadId = match.Groups[2].Value;
                string levelStr = match.Groups[3].Value;
                string message = match.Groups[4].Value;

                p.Inlines.Add(new Run(timeStr + " ") { Foreground = TimestampBrush });
                p.Inlines.Add(new Run(threadId + " ") { Foreground = MetadataBrush });

                string lowerLvl = levelStr.ToLowerInvariant();
                if (lowerLvl.Contains("error") || lowerLvl.Contains("fatal") || lowerLvl.Contains("crit"))
                {
                    level = LogLevel.Error;
                    stripeBrush = ErrorStripeBrush;
                    p.Inlines.Add(new Run($"[{levelStr}] ") { Foreground = ErrorBadgeBrush, FontWeight = FontWeights.Bold });
                    p.Inlines.Add(new Run(message) { Foreground = ErrorTextBrush });
                }
                else if (lowerLvl.Contains("warn"))
                {
                    level = LogLevel.Warning;
                    stripeBrush = WarnStripeBrush;
                    p.Inlines.Add(new Run($"[{levelStr}] ") { Foreground = WarnBadgeBrush, FontWeight = FontWeights.Bold });
                    p.Inlines.Add(new Run(message) { Foreground = WarnTextBrush });
                }
                else
                {
                    // [Note] / [Info] w MariaDB — wreszcie w czytelnym błękicie i jasnym tekście zamiast ciemnozielonego!
                    level = LogLevel.Info;
                    stripeBrush = TransparentBrush;
                    p.Inlines.Add(new Run($"[{levelStr}] ") { Foreground = NoteBadgeBrush, FontWeight = FontWeights.Bold });
                    p.Inlines.Add(new Run(message) { Foreground = PrimaryTextBrush });
                }

                ApplyParagraphBorders(p, stripeBrush);
                return new LogEntry { RawText = line, Level = level, Paragraph = p };
            }
        }
        else if (isApacheError)
        {
            var match = ApacheErrorRegex.Match(line);
            if (match.Success)
            {
                string timeStr = match.Groups[1].Value;
                string moduleLevel = match.Groups[2].Value;
                string pidStr = match.Groups[3].Value;
                string message = match.Groups[4].Value;

                p.Inlines.Add(new Run($"[{timeStr}] ") { Foreground = TimestampBrush });

                string lowerMod = moduleLevel.ToLowerInvariant();
                if (lowerMod.Contains(":error") || lowerMod.Contains(":crit") || lowerMod.Contains(":emerg") || lowerMod.Contains(":alert"))
                {
                    level = LogLevel.Error;
                    stripeBrush = ErrorStripeBrush;
                    p.Inlines.Add(new Run($"[{moduleLevel}] ") { Foreground = ErrorBadgeBrush, FontWeight = FontWeights.Bold });
                }
                else if (lowerMod.Contains(":warn"))
                {
                    level = LogLevel.Warning;
                    stripeBrush = WarnStripeBrush;
                    p.Inlines.Add(new Run($"[{moduleLevel}] ") { Foreground = WarnBadgeBrush, FontWeight = FontWeights.Bold });
                }
                else
                {
                    level = LogLevel.Info;
                    stripeBrush = TransparentBrush;
                    p.Inlines.Add(new Run($"[{moduleLevel}] ") { Foreground = NoticeBadgeBrush, FontWeight = FontWeights.Bold });
                }

                if (!string.IsNullOrEmpty(pidStr))
                {
                    p.Inlines.Add(new Run($"[{pidStr}] ") { Foreground = MetadataBrush });
                }

                Brush textBrush = level switch
                {
                    LogLevel.Error => ErrorTextBrush,
                    LogLevel.Warning => WarnTextBrush,
                    _ => PrimaryTextBrush
                };

                p.Inlines.Add(new Run(message) { Foreground = textBrush });

                ApplyParagraphBorders(p, stripeBrush);
                return new LogEntry { RawText = line, Level = level, Paragraph = p };
            }
        }
        else if (isAccessLog)
        {
            var match = ApacheAccessRegex.Match(line);
            if (match.Success)
            {
                string ip = match.Groups[1].Value;
                string timeStr = match.Groups[2].Value;
                string method = match.Groups[3].Value;
                string url = match.Groups[4].Value;
                string proto = match.Groups[5].Value;
                string statusCodeStr = match.Groups[6].Value;
                string bytesStr = match.Groups[7].Value;

                p.Inlines.Add(new Run(ip + " ") { Foreground = MetadataBrush });
                p.Inlines.Add(new Run($"[{timeStr}] ") { Foreground = TimestampBrush });

                Brush methodBrush = method.ToUpperInvariant() switch
                {
                    "GET" => MethodGetBrush,
                    "POST" => MethodPostBrush,
                    "PUT" or "PATCH" => MethodPutBrush,
                    "DELETE" => MethodDeleteBrush,
                    _ => MethodOtherBrush
                };

                p.Inlines.Add(new Run(method + " ") { Foreground = methodBrush, FontWeight = FontWeights.Bold });
                p.Inlines.Add(new Run(url + " ") { Foreground = PrimaryTextBrush });

                if (!string.IsNullOrEmpty(proto))
                {
                    p.Inlines.Add(new Run(proto + " ") { Foreground = MetadataBrush });
                }

                int.TryParse(statusCodeStr, out int statusCode);
                Brush statusBrush;

                if (statusCode >= 500)
                {
                    level = LogLevel.Error;
                    stripeBrush = ErrorStripeBrush;
                    statusBrush = ErrorBadgeBrush;
                }
                else if (statusCode >= 400)
                {
                    level = LogLevel.Warning;
                    stripeBrush = WarnStripeBrush;
                    statusBrush = WarnBadgeBrush;
                }
                else if (statusCode >= 300)
                {
                    level = LogLevel.Info;
                    statusBrush = NoticeBadgeBrush;
                }
                else
                {
                    level = LogLevel.Info;
                    statusBrush = SuccessBrush;
                }

                p.Inlines.Add(new Run(statusCodeStr + " ") { Foreground = statusBrush, FontWeight = FontWeights.Bold });
                p.Inlines.Add(new Run(bytesStr) { Foreground = MetadataBrush });

                ApplyParagraphBorders(p, stripeBrush);
                return new LogEntry { RawText = line, Level = level, Paragraph = p };
            }
        }

        // Generyczny fallback dla niestandardowych linii
        string lowerGeneric = line.ToLowerInvariant();
        if (lowerGeneric.Contains("error") || lowerGeneric.Contains("fatal") || lowerGeneric.Contains("crit") || lowerGeneric.Contains("exception"))
        {
            level = LogLevel.Error;
            stripeBrush = ErrorStripeBrush;
            p.Inlines.Add(new Run(line) { Foreground = ErrorTextBrush, FontWeight = FontWeights.Medium });
        }
        else if (lowerGeneric.Contains("warn"))
        {
            level = LogLevel.Warning;
            stripeBrush = WarnStripeBrush;
            p.Inlines.Add(new Run(line) { Foreground = WarnTextBrush });
        }
        else
        {
            level = LogLevel.Info;
            p.Inlines.Add(new Run(line) { Foreground = PrimaryTextBrush });
        }

        ApplyParagraphBorders(p, stripeBrush);
        return new LogEntry { RawText = line, Level = level, Paragraph = p };
    }

    private static void ApplyParagraphBorders(Paragraph p, Brush stripeBrush)
    {
        p.BorderThickness = new Thickness(3, 0, 0, 0);
        p.BorderBrush = stripeBrush;
        p.Padding = new Thickness(8, 1, 0, 1);
    }

    private bool MatchesFilter(LogEntry entry)
    {
        if (_currentFilterLevel == "error" && entry.Level != LogLevel.Error)
            return false;

        if (_currentFilterLevel == "warn_error" && entry.Level == LogLevel.Info)
            return false;

        if (!string.IsNullOrEmpty(_currentSearchQuery) &&
            !entry.RawText.Contains(_currentSearchQuery, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private void ApplyFilterToDocument()
    {
        if (!_isInitialized || DocLogs == null) return;

        DocLogs.BeginInit();
        try
        {
            DocLogs.Blocks.Clear();

            foreach (var entry in _allEntries)
            {
                if (MatchesFilter(entry))
                {
                    DocLogs.Blocks.Add(entry.Paragraph);
                }
            }
        }
        finally
        {
            DocLogs.EndInit();
        }

        UpdateLineCountText();

        if (ChkAutoScroll.IsChecked == true)
        {
            RtbLogContent.ScrollToEnd();
        }
    }

    private void UpdateLineCountText()
    {
        if (TxtLineCount == null || DocLogs == null) return;
        int visibleCount = DocLogs.Blocks.Count;
        int totalCount = _allEntries.Count;
        string template = TranslationSource.Instance["logsLinesCount"] ?? "Wyświetlono: {0} z {1} wpisów";
        TxtLineCount.Text = string.Format(template, visibleCount, totalCount);
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        _searchDebounceTimer.Stop();
        base.OnClosed(e);

        if (Owner != null)
        {
            if (Owner.WindowState == WindowState.Minimized)
            {
                Owner.WindowState = WindowState.Normal;
            }
            Owner.Activate();
        }
    }
}
