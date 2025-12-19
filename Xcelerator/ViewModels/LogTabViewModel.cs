using Xcelerator.Models;
using Xcelerator.LogEngine.Services;
using System.IO;

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

        /// <summary>
        /// Initializes a new instance of the LogTabViewModel class
        /// </summary>
        /// <param name="remoteMachineItem">The remote machine item to display logs for</param>
        public LogTabViewModel(RemoteMachineItem remoteMachineItem)
        {
            _headerName = remoteMachineItem.DisplayName;
            _remoteMachine = remoteMachineItem;
            _logHarvesterService = new LogHarvesterService();
            
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
                LogContent = "Loading logs...";
                
                var result = await _logHarvesterService.GetLogsInParallelAsync(machineName, itemName);
                
                if (result.Success && !string.IsNullOrEmpty(result.LocalFilePath))
                {
                    // Read the downloaded log file
                    var logLines = await File.ReadAllLinesAsync(result.LocalFilePath);
                    LogContent = string.Join(Environment.NewLine, logLines);
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
    }
}
