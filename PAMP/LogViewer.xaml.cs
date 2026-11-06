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
        public required bool IsAccessLog { get; init; }
        public required bool IsApacheError { get; init; }
        public required bool IsMariaDb { get; init; }
        public Paragraph? Paragraph { get; set; }
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

    private record class LogPalette(
        Brush PrimaryText,
        Brush Timestamp,
        Brush Metadata,
        Brush ErrorText,
        Brush ErrorBadge,
        Brush WarnText,
        Brush WarnBadge,
        Brush NoteBadge,
        Brush NoticeBadge,
        Brush Success,
        Brush MethodGet,
        Brush MethodPost,
        Brush MethodPut,
        Brush MethodDelete,
        Brush MethodOther,
        Brush ErrorStripe,
        Brush WarnStripe
    );

    // Paleta dla Dark Mode (wysoki kontrast na ciemnym tle)
    private static readonly LogPalette DarkPalette = new(
        PrimaryText: CreateFrozenBrush(Color.FromRgb(226, 232, 240)),  // Slate 200
        Timestamp: CreateFrozenBrush(Color.FromRgb(148, 163, 184)),    // Slate 400
        Metadata: CreateFrozenBrush(Color.FromRgb(100, 116, 139)),     // Slate 500
        ErrorText: CreateFrozenBrush(Color.FromRgb(248, 113, 113)),    // Red 400
        ErrorBadge: CreateFrozenBrush(Color.FromRgb(239, 68, 68)),     // Red 500
        WarnText: CreateFrozenBrush(Color.FromRgb(251, 191, 36)),      // Amber 400
        WarnBadge: CreateFrozenBrush(Color.FromRgb(245, 158, 11)),     // Amber 500
        NoteBadge: CreateFrozenBrush(Color.FromRgb(56, 189, 248)),     // Sky 400
        NoticeBadge: CreateFrozenBrush(Color.FromRgb(125, 211, 252)),  // Sky 300
        Success: CreateFrozenBrush(Color.FromRgb(74, 222, 128)),       // Green 400
        MethodGet: CreateFrozenBrush(Color.FromRgb(56, 189, 248)),     // Sky 400
        MethodPost: CreateFrozenBrush(Color.FromRgb(74, 222, 128)),    // Green 400
        MethodPut: CreateFrozenBrush(Color.FromRgb(251, 191, 36)),     // Amber 400
        MethodDelete: CreateFrozenBrush(Color.FromRgb(248, 113, 113)), // Red 400
        MethodOther: CreateFrozenBrush(Color.FromRgb(192, 132, 252)),  // Purple 400
        ErrorStripe: CreateFrozenBrush(Color.FromRgb(239, 68, 68)),    // Red 500
        WarnStripe: CreateFrozenBrush(Color.FromRgb(245, 158, 11))     // Amber 500
    );

    // Paleta dla Light Mode (maksymalna czytelność i wyrazisty kontrast na jasnym/białym tle)
    private static readonly LogPalette LightPalette = new(
        PrimaryText: CreateFrozenBrush(Color.FromRgb(30, 41, 59)),     // Slate 800 (wyraźny, ciemny antracyt)
        Timestamp: CreateFrozenBrush(Color.FromRgb(100, 116, 139)),    // Slate 500
        Metadata: CreateFrozenBrush(Color.FromRgb(71, 85, 105)),       // Slate 600
        ErrorText: CreateFrozenBrush(Color.FromRgb(220, 38, 38)),      // Red 600
        ErrorBadge: CreateFrozenBrush(Color.FromRgb(185, 28, 28)),     // Red 700
        WarnText: CreateFrozenBrush(Color.FromRgb(180, 83, 9)),        // Amber 700 (czytelny brązowo-bursztynowy)
        WarnBadge: CreateFrozenBrush(Color.FromRgb(146, 64, 14)),      // Amber 800
        NoteBadge: CreateFrozenBrush(Color.FromRgb(2, 132, 199)),      // Sky 600 (wyrazisty błękit)
        NoticeBadge: CreateFrozenBrush(Color.FromRgb(2, 132, 199)),    // Sky 600
        Success: CreateFrozenBrush(Color.FromRgb(22, 163, 74)),        // Green 600
        MethodGet: CreateFrozenBrush(Color.FromRgb(2, 132, 199)),      // Sky 600
        MethodPost: CreateFrozenBrush(Color.FromRgb(22, 163, 74)),     // Green 600
        MethodPut: CreateFrozenBrush(Color.FromRgb(180, 83, 9)),       // Amber 700
        MethodDelete: CreateFrozenBrush(Color.FromRgb(220, 38, 38)),   // Red 600
        MethodOther: CreateFrozenBrush(Color.FromRgb(126, 34, 206)),   // Purple 700
        ErrorStripe: CreateFrozenBrush(Color.FromRgb(220, 38, 38)),    // Red 600
        WarnStripe: CreateFrozenBrush(Color.FromRgb(217, 119, 6))      // Amber 600
    );

    private static LogPalette CurrentPalette => App.IsDarkTheme() ? DarkPalette : LightPalette;
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
        Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    private void OnUserPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
    {
        if (e.Category is Microsoft.Win32.UserPreferenceCategory.General or Microsoft.Win32.UserPreferenceCategory.Color)
        {
            Dispatcher.InvokeAsync(() =>
            {
                foreach (var entry in _allEntries)
                {
                    entry.Paragraph = null;
                }
                ApplyFilterToDocument();
            });
        }
    }

    private void InitializeLogs()
    {
        string logsDir = AppPaths.LogsDir;

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

                LogLevel level = DetermineLogLevel(line, isAccessLog, isApacheError, isMariaDb);
                newEntries.Add(new LogEntry
                {
                    RawText = line,
                    Level = level,
                    IsAccessLog = isAccessLog,
                    IsApacheError = isApacheError,
                    IsMariaDb = isMariaDb
                });
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
                            DocLogs.Blocks.Add(GetOrCreateParagraph(entry));
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

    private static LogLevel DetermineLogLevel(string line, bool isAccessLog, bool isApacheError, bool isMariaDb)
    {
        if (isMariaDb)
        {
            var match = MariaDbRegex.Match(line);
            if (match.Success)
            {
                string lowerLvl = match.Groups[3].Value.ToLowerInvariant();
                if (lowerLvl.Contains("error") || lowerLvl.Contains("fatal") || lowerLvl.Contains("crit")) return LogLevel.Error;
                if (lowerLvl.Contains("warn")) return LogLevel.Warning;
                return LogLevel.Info;
            }
        }
        else if (isApacheError)
        {
            var match = ApacheErrorRegex.Match(line);
            if (match.Success)
            {
                string lowerMod = match.Groups[2].Value.ToLowerInvariant();
                if (lowerMod.Contains(":error") || lowerMod.Contains(":crit") || lowerMod.Contains(":emerg") || lowerMod.Contains(":alert")) return LogLevel.Error;
                if (lowerMod.Contains(":warn")) return LogLevel.Warning;
                return LogLevel.Info;
            }
        }
        else if (isAccessLog)
        {
            var match = ApacheAccessRegex.Match(line);
            if (match.Success)
            {
                int.TryParse(match.Groups[6].Value, out int statusCode);
                if (statusCode >= 500) return LogLevel.Error;
                if (statusCode >= 400) return LogLevel.Warning;
                return LogLevel.Info;
            }
        }

        string lowerGeneric = line.ToLowerInvariant();
        if (lowerGeneric.Contains("error") || lowerGeneric.Contains("fatal") || lowerGeneric.Contains("crit") || lowerGeneric.Contains("exception"))
            return LogLevel.Error;
        if (lowerGeneric.Contains("warn"))
            return LogLevel.Warning;

        return LogLevel.Info;
    }

    private Paragraph GetOrCreateParagraph(LogEntry entry)
    {
        if (entry.Paragraph != null) return entry.Paragraph;

        var p = new Paragraph
        {
            Margin = new Thickness(0, 1.5, 0, 1.5),
            LineHeight = 19,
            FontFamily = ConsoleFont,
            FontSize = 12
        };

        var palette = CurrentPalette;
        Brush stripeBrush = TransparentBrush;
        string line = entry.RawText;

        if (entry.IsMariaDb)
        {
            var match = MariaDbRegex.Match(line);
            if (match.Success)
            {
                string timeStr = match.Groups[1].Value;
                string threadId = match.Groups[2].Value;
                string levelStr = match.Groups[3].Value;
                string message = match.Groups[4].Value;

                p.Inlines.Add(new Run(timeStr + " ") { Foreground = palette.Timestamp });
                p.Inlines.Add(new Run(threadId + " ") { Foreground = palette.Metadata });

                if (entry.Level == LogLevel.Error)
                {
                    stripeBrush = palette.ErrorStripe;
                    p.Inlines.Add(new Run($"[{levelStr}] ") { Foreground = palette.ErrorBadge, FontWeight = FontWeights.Bold });
                    p.Inlines.Add(new Run(message) { Foreground = palette.ErrorText });
                }
                else if (entry.Level == LogLevel.Warning)
                {
                    stripeBrush = palette.WarnStripe;
                    p.Inlines.Add(new Run($"[{levelStr}] ") { Foreground = palette.WarnBadge, FontWeight = FontWeights.Bold });
                    p.Inlines.Add(new Run(message) { Foreground = palette.WarnText });
                }
                else
                {
                    p.Inlines.Add(new Run($"[{levelStr}] ") { Foreground = palette.NoteBadge, FontWeight = FontWeights.Bold });
                    p.Inlines.Add(new Run(message) { Foreground = palette.PrimaryText });
                }

                ApplyParagraphBorders(p, stripeBrush);
                entry.Paragraph = p;
                return p;
            }
        }
        else if (entry.IsApacheError)
        {
            var match = ApacheErrorRegex.Match(line);
            if (match.Success)
            {
                string timeStr = match.Groups[1].Value;
                string moduleLevel = match.Groups[2].Value;
                string pidStr = match.Groups[3].Value;
                string message = match.Groups[4].Value;

                p.Inlines.Add(new Run($"[{timeStr}] ") { Foreground = palette.Timestamp });

                if (entry.Level == LogLevel.Error)
                {
                    stripeBrush = palette.ErrorStripe;
                    p.Inlines.Add(new Run($"[{moduleLevel}] ") { Foreground = palette.ErrorBadge, FontWeight = FontWeights.Bold });
                }
                else if (entry.Level == LogLevel.Warning)
                {
                    stripeBrush = palette.WarnStripe;
                    p.Inlines.Add(new Run($"[{moduleLevel}] ") { Foreground = palette.WarnBadge, FontWeight = FontWeights.Bold });
                }
                else
                {
                    p.Inlines.Add(new Run($"[{moduleLevel}] ") { Foreground = palette.NoticeBadge, FontWeight = FontWeights.Bold });
                }

                if (!string.IsNullOrEmpty(pidStr))
                {
                    p.Inlines.Add(new Run($"[{pidStr}] ") { Foreground = palette.Metadata });
                }

                Brush textBrush = entry.Level switch
                {
                    LogLevel.Error => palette.ErrorText,
                    LogLevel.Warning => palette.WarnText,
                    _ => palette.PrimaryText
                };

                p.Inlines.Add(new Run(message) { Foreground = textBrush });

                ApplyParagraphBorders(p, stripeBrush);
                entry.Paragraph = p;
                return p;
            }
        }
        else if (entry.IsAccessLog)
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

                p.Inlines.Add(new Run(ip + " ") { Foreground = palette.Metadata });
                p.Inlines.Add(new Run($"[{timeStr}] ") { Foreground = palette.Timestamp });

                Brush methodBrush = method.ToUpperInvariant() switch
                {
                    "GET" => palette.MethodGet,
                    "POST" => palette.MethodPost,
                    "PUT" or "PATCH" => palette.MethodPut,
                    "DELETE" => palette.MethodDelete,
                    _ => palette.MethodOther
                };

                p.Inlines.Add(new Run(method + " ") { Foreground = methodBrush, FontWeight = FontWeights.Bold });
                p.Inlines.Add(new Run(url + " ") { Foreground = palette.PrimaryText });

                if (!string.IsNullOrEmpty(proto))
                {
                    p.Inlines.Add(new Run(proto + " ") { Foreground = palette.Metadata });
                }

                Brush statusBrush;
                if (entry.Level == LogLevel.Error)
                {
                    stripeBrush = palette.ErrorStripe;
                    statusBrush = palette.ErrorBadge;
                }
                else if (entry.Level == LogLevel.Warning)
                {
                    stripeBrush = palette.WarnStripe;
                    statusBrush = palette.WarnBadge;
                }
                else
                {
                    int.TryParse(statusCodeStr, out int code);
                    statusBrush = code >= 300 ? palette.NoticeBadge : palette.Success;
                }

                p.Inlines.Add(new Run(statusCodeStr + " ") { Foreground = statusBrush, FontWeight = FontWeights.Bold });
                p.Inlines.Add(new Run(bytesStr) { Foreground = palette.Metadata });

                ApplyParagraphBorders(p, stripeBrush);
                entry.Paragraph = p;
                return p;
            }
        }

        // Generyczny fallback dla niestandardowych linii
        if (entry.Level == LogLevel.Error)
        {
            stripeBrush = palette.ErrorStripe;
            p.Inlines.Add(new Run(line) { Foreground = palette.ErrorText, FontWeight = FontWeights.Medium });
        }
        else if (entry.Level == LogLevel.Warning)
        {
            stripeBrush = palette.WarnStripe;
            p.Inlines.Add(new Run(line) { Foreground = palette.WarnText });
        }
        else
        {
            p.Inlines.Add(new Run(line) { Foreground = palette.PrimaryText });
        }

        ApplyParagraphBorders(p, stripeBrush);
        entry.Paragraph = p;
        return p;
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
                    DocLogs.Blocks.Add(GetOrCreateParagraph(entry));
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
        Microsoft.Win32.SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
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
