using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Xml.Serialization;
using Xcelerator.LogEngine.Services;
using Xcelerator.Models;
using Xcelerator.Services;

namespace Xcelerator.ViewModels
{
    /// <summary>
    /// ViewModel for a log tab that displays logs for a specific remote machine
    /// </summary>
    public class LogTabViewModel : BaseViewModel, ITabViewModel
    {
        private string _headerName;
        private RemoteMachineItem? _remoteMachine;
        private string _logContent = string.Empty;
        private readonly LogHarvesterServiceAdvanced _logHarvesterService;
        private readonly LogFileManager _logFileManager;
        private ObservableCollection<string> _logLines;
        private bool _isLoading;
        private string? _localFilePath;
        private string? _selectedLogLine;
        private bool _isDetailPanelVisible;
        private int _refreshIntervalMinutes;
        private System.Threading.Timer? _refreshTimer;
        private bool _isRefreshing;
        private string _machineName = string.Empty;
        private string _machineItemName = string.Empty;
        private double _loadTimeSeconds;
        private bool _isRefreshDropdownEnabled;
        private ObservableCollection<HighlightSetting> _highlightSettings;
        private bool _isHighlightPanelVisible;
        private HighlightSetting? _selectedHighlight;
        private string _searchText = string.Empty;
        private int _matchCount;

        /// <summary>
        /// Initializes a new instance of the LogTabViewModel class
        /// </summary>
        /// <param name="remoteMachineItem">The remote machine item to display logs for</param>
        /// <param name="logFileManager">The log file manager service</param>
        /// <param name="machineName">The machine name (e.g., "SOA-C30COR01")</param>
        /// <param name="machineItemName">The machine item name (e.g., "VC")</param>
        public LogTabViewModel(
            RemoteMachineItem remoteMachineItem, 
            LogFileManager logFileManager, 
            string? machineName = null,
            string? machineItemName = null)
        {
            _headerName = remoteMachineItem.Name;
            _remoteMachine = remoteMachineItem;
            _logFileManager = logFileManager ?? throw new ArgumentNullException(nameof(logFileManager));
            _logHarvesterService = new LogHarvesterServiceAdvanced();
            _logLines = new ObservableCollection<string>();

            _machineName = machineName ?? string.Empty;
            _machineItemName = machineItemName ?? string.Empty;

            // Initialize highlight settings
            _highlightSettings = new ObservableCollection<HighlightSetting>();
            LoadHighlightSettings();

            // Load logs asynchronously
            _ = LoadLogsAsync(machineName, machineItemName);
        }

        /// <summary>
        /// Loads logs from the remote machine asynchronously with retry logic
        /// </summary>
        /// <param name="machineName">The machine name (e.g., "SO-C30COR01")</param>
        /// <param name="itemName">The item name (e.g., "VC")</param>
        private async Task LoadLogsAsync(string machineName, string itemName)
        {
            const int maxRetries = 3;
            int attemptNumber = 0;
            Exception? lastException = null;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                IsLoading = true;
                IsRefreshDropdownEnabled = false;
                
                while (attemptNumber < maxRetries)
                {
                    attemptNumber++;
                    
                    try
                    {
                        LogContent = attemptNumber == 1 
                            ? "Loading logs..." 
                            : $"Retrying... (Attempt {attemptNumber} of {maxRetries})";
                        
                        // Perform heavy work on background thread
                        var result = await Task.Run(async () =>
                        {
                            return await _logHarvesterService.GetLogsInParallelAsync(machineName, itemName);
                        });
                        
                        if (result.Success && !string.IsNullOrEmpty(result.LocalFilePath))
                        {
                            // Store the local file path for cleanup later
                            LocalFilePath = result.LocalFilePath;
                            
                            // Register log file with centralized manager for automatic cleanup
                            _logFileManager.RegisterLogFile(result.LocalFilePath);
                            
                            // Read file and populate collection in chunks to avoid UI freeze
                            await LoadLogLinesInChunks(result.LocalFilePath, stopwatch);
                            
                            // Success - exit retry loop
                            return;
                        }
                        else
                        {
                            // Create exception for failed result to trigger retry
                            lastException = new Exception(result.ErrorMessage ?? "Unknown error");
                            
                            // If this isn't the last attempt, wait before retrying
                            if (attemptNumber < maxRetries)
                            {
                                await Task.Delay(TimeSpan.FromSeconds(2 * attemptNumber)); // Exponential backoff: 2s, 4s, 6s
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        
                        // If this isn't the last attempt, wait before retrying
                        if (attemptNumber < maxRetries)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(2 * attemptNumber)); // Exponential backoff: 2s, 4s, 6s
                        }
                    }
                }
                
                // If we get here, all retries failed
                LogContent = $"Failed to load logs after {maxRetries} attempts: {lastException?.Message ?? "Unknown error"}";
            }
            catch (Exception ex)
            {
                LogContent = $"Error loading logs: {ex.Message}";
            }
            finally
            {
                stopwatch.Stop();
                LoadTimeSeconds = stopwatch.Elapsed.TotalSeconds;
                IsLoading = false;
                IsRefreshDropdownEnabled = true;
            }
        }

        /// <summary>
        /// Checks if a line starts with a timestamp in the format MM/dd/yyyy HH:mm:ss.fff
        /// </summary>
        private bool StartsWithTimestamp(string line)
        {
            if (string.IsNullOrEmpty(line) || line.Length < 23)
                return false;

            // Check format: MM/dd/yyyy HH:mm:ss.fff
            // Positions:    01234567890123456789012
            return char.IsDigit(line[0]) && char.IsDigit(line[1]) && line[2] == '/' &&
                   char.IsDigit(line[3]) && char.IsDigit(line[4]) && line[5] == '/' &&
                   char.IsDigit(line[6]) && char.IsDigit(line[7]) && 
                   char.IsDigit(line[8]) && char.IsDigit(line[9]) && line[10] == ' ' &&
                   char.IsDigit(line[11]) && char.IsDigit(line[12]) && line[13] == ':' &&
                   char.IsDigit(line[14]) && char.IsDigit(line[15]) && line[16] == ':' &&
                   char.IsDigit(line[17]) && char.IsDigit(line[18]) && line[19] == '.';
        }

        /// <summary>
        /// Loads log lines in chunks to prevent UI freezing
        /// Optimized version: streams file, batches additions, minimizes dispatcher calls
        /// Joins multi-line log entries (lines without timestamps are appended to previous entry)
        /// </summary>
        /// <param name="filePath">Path to the log file</param>
        /// <param name="stopwatch">Stopwatch to track total operation time</param>
        private async Task LoadLogLinesInChunks(string filePath, System.Diagnostics.Stopwatch stopwatch)
        {
            const int chunkSize = 5000; // Larger chunks for fewer dispatcher calls
            const int bufferSize = 65536; // 64KB buffer for file reading
            
            var buffer = new List<string>(chunkSize);
            int totalLinesLoaded = 0;
            string? currentEntry = null;
            
            // Stream file line-by-line instead of loading all into memory
            await Task.Run(async () =>
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
                using var reader = new StreamReader(stream);
                
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    // Check if this line starts a new log entry (has timestamp)
                    if (StartsWithTimestamp(line))
                    {
                        // Save the previous complete entry if exists
                        if (currentEntry != null)
                        {
                            buffer.Add(currentEntry);
                        }
                        
                        // Start a new entry
                        currentEntry = line;
                    }
                    else
                    {
                        // This is a continuation line - append to current entry
                        if (currentEntry != null)
                        {
                            currentEntry += Environment.NewLine + line;
                        }
                        else
                        {
                            // No current entry yet (file doesn't start with timestamp)
                            // Start with this line
                            currentEntry = line;
                        }
                    }
                    
                    // When buffer is full, batch update the UI
                    if (buffer.Count >= chunkSize)
                    {
                        var chunk = buffer.ToArray();
                        buffer.Clear();
                        totalLinesLoaded += chunk.Length;
                        
                        // Single dispatcher call per chunk with batch addition
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            // Temporarily disable collection change notifications for performance
                            foreach (var l in chunk)
                            {
                                LogLines.Add(l);
                            }
                            LogContent = $"Loaded {totalLinesLoaded:N0} entries...";
                        }, System.Windows.Threading.DispatcherPriority.Background);
                    }
                }
                
                // Don't forget the last entry
                if (currentEntry != null)
                {
                    buffer.Add(currentEntry);
                }
                
                // Process remaining lines
                if (buffer.Count > 0)
                {
                    var chunk = buffer.ToArray();
                    totalLinesLoaded += chunk.Length;
                    
                    stopwatch.Stop();
                    var elapsed = stopwatch.Elapsed;
                    
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var l in chunk)
                        {
                            LogLines.Add(l);
                        }
                        
                        // Format elapsed time nicely
                        string timeDisplay = elapsed.TotalSeconds < 1
                            ? $"{elapsed.TotalMilliseconds:F0}ms"
                            : elapsed.TotalMinutes >= 1
                                ? $"{elapsed.Minutes}m {elapsed.Seconds}s"
                                : $"{elapsed.TotalSeconds:F1}s";
                        
                        LogContent = $"Loaded {totalLinesLoaded:N0} log entries from {Path.GetFileName(filePath)} in {timeDisplay}";
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
            });
        }

        /// <summary>
        /// The display name shown in the tab header
        /// </summary>
        public string HeaderName
        {
            get => _headerName;
            set => SetProperty(ref _headerName, value);
        }

        /// <summary>
        /// The remote machine item associated with this log tab
        /// </summary>
        public RemoteMachineItem? RemoteMachine
        {
            get => _remoteMachine;
            set => SetProperty(ref _remoteMachine, value);
        }

        /// <summary>
        /// The log content to display in this tab
        /// </summary>
        public string LogContent
        {
            get => _logContent;
            set => SetProperty(ref _logContent, value);
        }

        /// <summary>
        /// Collection of log lines for efficient rendering
        /// </summary>
        public ObservableCollection<string> LogLines
        {
            get => _logLines;
            set => SetProperty(ref _logLines, value);
        }

        /// <summary>
        /// Indicates whether logs are currently being loaded
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Path to the local temporary log file
        /// </summary>
        public string? LocalFilePath
        {
            get => _localFilePath;
            set => SetProperty(ref _localFilePath, value);
        }

        /// <summary>
        /// The currently selected log line to display in detail panel
        /// </summary>
        public string? SelectedLogLine
        {
            get => _selectedLogLine;
            set
            {
                if (SetProperty(ref _selectedLogLine, value))
                {
                    // Show detail panel when a line is selected
                    IsDetailPanelVisible = !string.IsNullOrEmpty(value);
                }
            }
        }

        /// <summary>
        /// Indicates whether the detail panel is visible
        /// </summary>
        public bool IsDetailPanelVisible
        {
            get => _isDetailPanelVisible;
            set => SetProperty(ref _isDetailPanelVisible, value);
        }

        /// <summary>
        /// The refresh interval in minutes (0 = disabled)
        /// </summary>
        public int RefreshIntervalMinutes
        {
            get => _refreshIntervalMinutes;
            set
            {
                if (SetProperty(ref _refreshIntervalMinutes, value))
                {
                    UpdateRefreshTimer();
                }
            }
        }

        /// <summary>
        /// Indicates whether logs are currently being refreshed
        /// </summary>
        public bool IsRefreshing
        {
            get => _isRefreshing;
            private set => SetProperty(ref _isRefreshing, value);
        }

        /// <summary>
        /// The time it took to load logs in seconds
        /// </summary>
        public double LoadTimeSeconds
        {
            get => _loadTimeSeconds;
            private set => SetProperty(ref _loadTimeSeconds, value);
        }

        /// <summary>
        /// Indicates whether the refresh interval dropdown is enabled (after initial load)
        /// </summary>
        public bool IsRefreshDropdownEnabled
        {
            get => _isRefreshDropdownEnabled;
            private set => SetProperty(ref _isRefreshDropdownEnabled, value);
        }

        /// <summary>
        /// Collection of highlight settings for color-coded log filtering
        /// </summary>
        public ObservableCollection<HighlightSetting> HighlightSettings
        {
            get => _highlightSettings;
            set => SetProperty(ref _highlightSettings, value);
        }

        /// <summary>
        /// Indicates whether the highlight panel is visible
        /// </summary>
        public bool IsHighlightPanelVisible
        {
            get => _isHighlightPanelVisible;
            set => SetProperty(ref _isHighlightPanelVisible, value);
        }

        /// <summary>
        /// The currently selected highlight setting
        /// </summary>
        public HighlightSetting? SelectedHighlight
        {
            get => _selectedHighlight;
            set
            {
                if (SetProperty(ref _selectedHighlight, value))
                {
                    UpdateHighlightSelection(value);
                    UpdateMatchCount();
                }
            }
        }

        /// <summary>
        /// The search text for highlighting matches in log lines
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    UpdateMatchCount();
                }
            }
        }

        /// <summary>
        /// The number of lines matching the search text
        /// </summary>
        public int MatchCount
        {
            get => _matchCount;
            private set => SetProperty(ref _matchCount, value);
        }

        /// <summary>
        /// Gets the minimum refresh interval in minutes based on load time (load time + 1 minute)
        /// </summary>
        public int MinimumRefreshIntervalMinutes
        {
            get
            {
                // Convert load time to minutes and round up, then add 1 minute buffer
                int loadTimeMinutes = (int)Math.Ceiling(LoadTimeSeconds / 60.0);
                return loadTimeMinutes + 1;
            }
        }

        /// <summary>
        /// Updates the refresh timer based on the RefreshIntervalMinutes property
        /// </summary>
        private void UpdateRefreshTimer()
        {
            // Stop and dispose existing timer
            if (_refreshTimer != null)
            {
                _refreshTimer.Dispose();
                _refreshTimer = null;
            }

            // If interval is 0 or negative, timer is disabled
            if (_refreshIntervalMinutes <= 0)
            {
                return;
            }

            // Create new timer with the specified interval
            var intervalMilliseconds = _refreshIntervalMinutes * 60 * 1000;
            _refreshTimer = new System.Threading.Timer(
                async _ => await RefreshLogsAsync(),
                null,
                intervalMilliseconds,
                intervalMilliseconds
            );
        }

        /// <summary>
        /// Refreshes logs in the background without replacing previous content until download is complete
        /// </summary>
        private async Task RefreshLogsAsync()
        {
            // Prevent multiple simultaneous refreshes
            if (_isRefreshing || IsLoading)
            {
                return;
            }

            try
            {
                IsRefreshing = true;

                // Download logs in background
                var result = await Task.Run(async () =>
                {
                    return await _logHarvesterService.GetLogsInParallelAsync(_machineName, _machineItemName);
                });

                if (result.Success && !string.IsNullOrEmpty(result.LocalFilePath))
                {
                    // Load new log lines into a temporary collection
                    var newLogLines = new ObservableCollection<string>();
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                    await LoadLogLinesInChunksToCollection(result.LocalFilePath, newLogLines, stopwatch);

                    // Replace content atomically on UI thread
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        // Clean up old file
                        if (!string.IsNullOrEmpty(LocalFilePath) && LocalFilePath != result.LocalFilePath)
                        {
                            _logFileManager.RemoveLogFile(LocalFilePath, deleteFile: true);
                        }

                        // Replace collection and update file path
                        LogLines = newLogLines;
                        LocalFilePath = result.LocalFilePath;
                        _logFileManager.RegisterLogFile(result.LocalFilePath);

                        // Update status
                        var timeDisplay = stopwatch.Elapsed.TotalSeconds < 1
                            ? $"{stopwatch.Elapsed.TotalMilliseconds:F0}ms"
                            : stopwatch.Elapsed.TotalMinutes >= 1
                                ? $"{stopwatch.Elapsed.Minutes}m {stopwatch.Elapsed.Seconds}s"
                                : $"{stopwatch.Elapsed.TotalSeconds:F1}s";

                        LogContent = $"Refreshed {newLogLines.Count:N0} log entries from {Path.GetFileName(result.LocalFilePath)} in {timeDisplay} (Last refresh: {DateTime.Now:HH:mm:ss})";
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    LogContent = $"Error refreshing logs: {ex.Message}";
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// Loads log lines into a specific collection (used for refresh without replacing UI until complete)
        /// </summary>
        private async Task LoadLogLinesInChunksToCollection(string filePath, ObservableCollection<string> targetCollection, System.Diagnostics.Stopwatch stopwatch)
        {
            const int bufferSize = 65536; // 64KB buffer for file reading
            string? currentEntry = null;

            await Task.Run(async () =>
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
                using var reader = new StreamReader(stream);

                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    // Check if this line starts a new log entry (has timestamp)
                    if (StartsWithTimestamp(line))
                    {
                        // Save the previous complete entry if exists
                        if (currentEntry != null)
                        {
                            targetCollection.Add(currentEntry);
                        }

                        // Start a new entry
                        currentEntry = line;
                    }
                    else
                    {
                        // This is a continuation line - append to current entry
                        if (currentEntry != null)
                        {
                            currentEntry += Environment.NewLine + line;
                        }
                        else
                        {
                            // No current entry yet (file doesn't start with timestamp)
                            currentEntry = line;
                        }
                    }
                }

                // Don't forget the last entry
                if (currentEntry != null)
                {
                    targetCollection.Add(currentEntry);
                }

                stopwatch.Stop();
            });
        }

        /// <summary>
        /// Scrolls to a specific line number in the log
        /// </summary>
        /// <param name="lineNumber">The line number (0-based index) to scroll to</param>
        public void ScrollToLine(int lineNumber)
        {
            if (lineNumber < 0 || lineNumber >= LogLines.Count)
            {
                System.Diagnostics.Debug.WriteLine($"Line number {lineNumber} is out of range (0-{LogLines.Count - 1})");
                return;
            }

            // Set the selected log line to the target line
            SelectedLogLine = LogLines[lineNumber];
        }

        /// <summary>
        /// Loads highlight settings from embedded XML or default configuration
        /// </summary>
        private void LoadHighlightSettings()
        {
            try
            {
                // For now, use the hardcoded XML from the requirement
                // In production, this could load from a file or user preferences
                string xmlContent = @"<?xml version=""1.0""?>
<HighlightSettingContainer xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <Items>
    <HighlightSetting>
      <BackColor>-7876870</BackColor>
      <BorderColor>-7876885</BorderColor>
      <MarkerColor>-7876885</MarkerColor>
      <Flags>5</Flags>
    </HighlightSetting>
    <HighlightSetting>
      <BackColor>-13447886</BackColor>
      <BorderColor>-16744448</BorderColor>
      <MarkerColor>-16744448</MarkerColor>
      <Flags>5</Flags>
    </HighlightSetting>
    <HighlightSetting>
      <BackColor>-18751</BackColor>
      <BorderColor>-65536</BorderColor>
      <MarkerColor>-65536</MarkerColor>
      <Flags>5</Flags>
    </HighlightSetting>
    <HighlightSetting>
      <BackColor>-2894893</BackColor>
      <BorderColor>-8355712</BorderColor>
      <MarkerColor>-8355712</MarkerColor>
      <Flags>5</Flags>
    </HighlightSetting>
    <HighlightSetting>
      <BackColor>-32</BackColor>
      <BorderColor>-256</BorderColor>
      <MarkerColor>-256</MarkerColor>
      <Flags>5</Flags>
    </HighlightSetting>
    <HighlightSetting>
      <BackColor>-7876870</BackColor>
      <BorderColor>-7876885</BorderColor>
      <MarkerColor>-7876885</MarkerColor>
      <Flags>5</Flags>
    </HighlightSetting>
    <HighlightSetting>
      <BackColor>-13447886</BackColor>
      <BorderColor>-16744448</BorderColor>
      <MarkerColor>-16744448</MarkerColor>
      <Flags>5</Flags>
    </HighlightSetting>
    <HighlightSetting>
      <BackColor>-18751</BackColor>
      <BorderColor>-65536</BorderColor>
      <MarkerColor>-65536</MarkerColor>
      <Flags>5</Flags>
    </HighlightSetting>
    <HighlightSetting>
      <BackColor>-2894893</BackColor>
      <BorderColor>-8355712</BorderColor>
      <MarkerColor>-8355712</MarkerColor>
      <Flags>5</Flags>
    </HighlightSetting>
    <HighlightSetting>
      <BackColor>-32</BackColor>
      <BorderColor>-256</BorderColor>
      <MarkerColor>-256</MarkerColor>
      <Flags>5</Flags>
    </HighlightSetting>
  </Items>
</HighlightSettingContainer>";

                var serializer = new XmlSerializer(typeof(HighlightSettingContainer));
                using var stringReader = new StringReader(xmlContent);
                var container = (HighlightSettingContainer?)serializer.Deserialize(stringReader);

                if (container?.Items != null)
                {
                    int index = 1;
                    foreach (var xmlSetting in container.Items)
                    {
                        var setting = xmlSetting.ToHighlightSetting();
                        setting.Name = $"Color {index}";
                        HighlightSettings.Add(setting);
                        index++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading highlight settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the selection state of highlights (only one can be selected at a time)
        /// </summary>
        private void UpdateHighlightSelection(HighlightSetting? newSelection)
        {
            foreach (var setting in HighlightSettings)
            {
                setting.IsSelected = setting == newSelection;
            }

            OnPropertyChanged(nameof(HighlightSettings));
        }

        /// <summary>
        /// Toggles the visibility of the highlight panel
        /// </summary>
        public void ToggleHighlightPanel()
        {
            IsHighlightPanelVisible = !IsHighlightPanelVisible;
        }

        /// <summary>
        /// Updates the match count based on current search text
        /// </summary>
        private void UpdateMatchCount()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                MatchCount = 0;
                return;
            }

            int count = 0;
            foreach (var line in LogLines)
            {
                if (line.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }
            MatchCount = count;
        }

        /// <summary>
        /// Clears the search and highlights
        /// </summary>
        public void ClearSearch()
        {
            SearchText = string.Empty;
            SelectedHighlight = null;
        }

        /// <summary>
        /// Cleans up resources including the temporary log file
        /// The file is also tracked by LogFileManager for application-wide cleanup
        /// </summary>
        public void Cleanup()
        {
            try
            {
                // Dispose timer
                if (_refreshTimer != null)
                {
                    _refreshTimer.Dispose();
                    _refreshTimer = null;
                }

                if (!string.IsNullOrEmpty(LocalFilePath))
                {
                    // Remove from log manager and delete immediately
                    _logFileManager.RemoveLogFile(LocalFilePath, deleteFile: true);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - cleanup is best effort
                System.Diagnostics.Debug.WriteLine($"Error cleaning up log file: {ex.Message}");
            }
        }
    }
}
