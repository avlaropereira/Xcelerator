using Xcelerator.Models;
using Xcelerator.LogEngine.Services;
using System.IO;
using System.Windows;
using System.Collections.ObjectModel;

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
        private ObservableCollection<string> _logLines;
        private bool _isLoading;
        private string? _localFilePath;
        private string? _selectedLogLine;
        private bool _isDetailPanelVisible;
        private string? _clusterName; // Track cluster name for log file registration

        /// <summary>
        /// Initializes a new instance of the LogTabViewModel class
        /// </summary>
        /// <param name="remoteMachineItem">The remote machine item to display logs for</param>
        /// <param name="clusterName">The cluster name for log file tracking (optional)</param>
        public LogTabViewModel(RemoteMachineItem remoteMachineItem, string? clusterName = null)
        {
            _headerName = remoteMachineItem.DisplayName;
            _remoteMachine = remoteMachineItem;
            _clusterName = clusterName;
            _logHarvesterService = new LogHarvesterServiceAdvanced();
            _logLines = new ObservableCollection<string>();
            
            // Parse machine name and item from the remote machine item name
            var (machineName, machineItemName) = ParseMachineItem(remoteMachineItem.Name);
            
            // Load logs asynchronously
            _ = LoadLogsAsync(machineName, machineItemName);
        }

        /// <summary>
        /// Loads logs from the remote machine asynchronously
        /// </summary>
        /// <param name="machineName">The machine name (e.g., "SO-C30COR01")</param>
        /// <param name="itemName">The item name (e.g., "VC")</param>
        private async Task LoadLogsAsync(string machineName, string itemName)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                IsLoading = true;
                LogContent = "Loading logs...";
                
                // Perform heavy work on background thread
                var result = await Task.Run(async () =>
                {
                    return await _logHarvesterService.GetLogsInParallelAsync(machineName, itemName);
                });
                
                if (result.Success && !string.IsNullOrEmpty(result.LocalFilePath))
                {
                    // Store the local file path for cleanup later
                    LocalFilePath = result.LocalFilePath;
                    
                    // Register log file with cluster for tracking
                    if (!string.IsNullOrEmpty(_clusterName))
                    {
                        PanelViewModel.RegisterLogFile(_clusterName, result.LocalFilePath);
                    }
                    
                    // Read file and populate collection in chunks to avoid UI freeze
                    await LoadLogLinesInChunks(result.LocalFilePath, stopwatch);
                }
                else
                {
                    LogContent = $"Failed to load logs: {result.ErrorMessage ?? "Unknown error"}";
                }
            }
            catch (Exception ex)
            {
                LogContent = $"Error loading logs: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
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
        /// Parses a machine-item string in the format "SO-C30COR01-VirtualCluster" 
        /// and returns the machine name and item abbreviation
        /// </summary>
        /// <param name="machineItemString">The string to parse (e.g., "SO-C30COR01-VirtualCluster")</param>
        /// <returns>A tuple containing the machine name (e.g., "SO-C30COR01") and item abbreviation (e.g., "VC")</returns>
        private (string MachineName, string ItemAbbreviation) ParseMachineItem(string machineItemString)
        {
            if (string.IsNullOrWhiteSpace(machineItemString))
            {
                throw new ArgumentException("Machine-item string cannot be null or empty", nameof(machineItemString));
            }

            // Find the second occurrence of '-'
            int firstDashIndex = machineItemString.IndexOf('-');
            if (firstDashIndex == -1)
            {
                throw new ArgumentException($"Invalid format: '{machineItemString}'. Expected format: 'SO-C30COR01-VirtualCluster'", nameof(machineItemString));
            }

            int secondDashIndex = machineItemString.IndexOf('-', firstDashIndex + 1);
            if (secondDashIndex == -1)
            {
                throw new ArgumentException($"Invalid format: '{machineItemString}'. Expected format: 'SO-C30COR01-VirtualCluster'", nameof(machineItemString));
            }

            // Extract machine name (everything before second dash)
            string machineName = machineItemString.Substring(0, secondDashIndex);

            // Extract item name (everything after second dash)
            string itemName = machineItemString.Substring(secondDashIndex + 1);

            Dictionary<string, string> remoteMachines = new Dictionary<string, string>();
            remoteMachines.Add("VirtualCluster", "VC");
            remoteMachines.Add("FileServer", "FileServer");
            remoteMachines.Add("CoOpService", "CoOp");
            remoteMachines.Add("SurvyService", "Surveys");
            remoteMachines.Add("FSDrivePublisher", "FileServerSetUp");
            remoteMachines.Add("DroneLetter", "DroveSvc");
            remoteMachines.Add("DBCWS", "Not Available");
            // API machines
            remoteMachines.Add("L7Healthcheck", "Not Available");
            remoteMachines.Add("DroneService", "Not Available");
            remoteMachines.Add("APIWebsite", "API");
            remoteMachines.Add("AutoSite", "Not Available");
            //remoteMachines.Add("DBCWS", "DBCWS");
            // WEB machines
            remoteMachines.Add("Agent", "Agent");
            remoteMachines.Add("AuthenticationServer", "AuthorizationServer");
            remoteMachines.Add("CacheSite", "CacheSite");
            remoteMachines.Add("inContact", "inContact");
            remoteMachines.Add("inControl", "inControl");
            remoteMachines.Add("ReportService", "ReportService");
            remoteMachines.Add("Security", "Not Available");
            remoteMachines.Add("WebScripting", "WebScripting");
            //remoteMachines.Add("DBCWS", "DBCWS");

            string itemAbbreviation = remoteMachines.TryGetValue(itemName, out var abbr) ? abbr : itemName;
            return (machineName, itemAbbreviation);
        }

        /// <summary>
        /// Cleans up resources including the temporary log file
        /// </summary>
        public void Cleanup()
        {
            try
            {
                if (!string.IsNullOrEmpty(LocalFilePath) && File.Exists(LocalFilePath))
                {
                    // Delete the log file
                    File.Delete(LocalFilePath);
                    
                    // Try to delete the parent directory if it's empty
                    var directory = Path.GetDirectoryName(LocalFilePath);
                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                    {
                        // Only delete if directory is empty
                        if (!Directory.EnumerateFileSystemEntries(directory).Any())
                        {
                            Directory.Delete(directory);
                        }
                    }
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
