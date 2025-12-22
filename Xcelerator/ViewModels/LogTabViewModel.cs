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
        private readonly LogHarvesterService _logHarvesterService;
        private ObservableCollection<string> _logLines;
        private bool _isLoading;
        private string? _localFilePath;
        private string? _selectedLogLine;
        private bool _isDetailPanelVisible;

        /// <summary>
        /// Initializes a new instance of the LogTabViewModel class
        /// </summary>
        /// <param name="remoteMachineItem">The remote machine item to display logs for</param>
        public LogTabViewModel(RemoteMachineItem remoteMachineItem)
        {
            _headerName = remoteMachineItem.DisplayName;
            _remoteMachine = remoteMachineItem;
            _logHarvesterService = new LogHarvesterService();
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
                    
                    // Read file and populate collection in chunks to avoid UI freeze
                    await LoadLogLinesInChunks(result.LocalFilePath);
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
        /// Loads log lines in chunks to prevent UI freezing
        /// </summary>
        private async Task LoadLogLinesInChunks(string filePath)
        {
            const int chunkSize = 1000; // Load 1000 lines at a time
            
            await Task.Run(async () =>
            {
                var lines = await File.ReadAllLinesAsync(filePath);
                
                // Process in chunks
                for (int i = 0; i < lines.Length; i += chunkSize)
                {
                    var chunk = lines.Skip(i).Take(chunkSize).ToArray();
                    
                    // Update UI on UI thread
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var line in chunk)
                        {
                            LogLines.Add(line);
                        }
                        
                        // Update summary content
                        LogContent = $"Loaded {LogLines.Count:N0} lines...";
                    }, System.Windows.Threading.DispatcherPriority.Background);
                    
                    // Small delay to let UI breathe
                    await Task.Delay(1);
                }
                
                // Final update
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    LogContent = $"Loaded {LogLines.Count:N0} log lines from {Path.GetFileName(filePath)}";
                });
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
