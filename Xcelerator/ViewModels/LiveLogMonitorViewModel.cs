using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Input;
using Xcelerator.Models;

namespace Xcelerator.ViewModels
{
    /// <summary>
    /// ViewModel for the LiveLogMonitorView that manages log monitoring functionality
    /// </summary>
    public class LiveLogMonitorViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private readonly DashboardViewModel _dashboardViewModel;
        private readonly Cluster? _cluster;
        private Dictionary<string, string>? _tokenData;
        private string _logContent = string.Empty;
        private string _status = "Ready";
        private bool _hasLogs = false;
        private string _searchText = string.Empty;
        private bool _isRegexMode = false;
        private int _matchCount = 0;
        private string? _selectedMachine;
        private ObservableCollection<string> _allMachines;
        private ICollectionView? _filteredMachines;
        private ObservableCollection<RemoteMachineItem> _remoteMachines;
        private RemoteMachineItem? _selectedRemoteMachine;

        public LiveLogMonitorViewModel(MainViewModel mainViewModel, DashboardViewModel dashboardViewModel, Cluster? cluster = null, Dictionary<string, string>? tokenData = null)
        {
            _mainViewModel = mainViewModel;
            _dashboardViewModel = dashboardViewModel;
            _cluster = cluster;
            _tokenData = tokenData;

            // Initialize machine list with sample data
            _allMachines = new ObservableCollection<string>();
            InitializeMachineList();

            // Initialize remote machines dynamically based on cluster name
            _remoteMachines = new ObservableCollection<RemoteMachineItem>();
            InitializeRemoteMachines();

            // Setup filtered collection view
            _filteredMachines = CollectionViewSource.GetDefaultView(_allMachines);
            if (_filteredMachines != null)
            {
                _filteredMachines.Filter = MachineFilter;
            }
            UpdateMatchCount();

            // Set initial status
            Status = "Ready";
        }

        #region Properties

        /// <summary>
        /// Decoded token data
        /// </summary>
        public Dictionary<string, string>? TokenData
        {
            get => _tokenData;
            private set => SetProperty(ref _tokenData, value);
        }

        /// <summary>
        /// User name from token
        /// </summary>
        public string UserName
        {
            get
            {
                if (_tokenData == null) return "User";
                if (_tokenData.TryGetValue("name", out var name)) return name;
                if (_tokenData.TryGetValue("given_name", out var givenName)) return givenName;
                return "User";
            }
        }

        /// <summary>
        /// Agent ID from token
        /// </summary>
        public string IcAgentId
        {
            get
            {
                if (_tokenData == null) return "N/A";
                if (_tokenData.TryGetValue("icAgentId", out var agentId)) return agentId;
                return "N/A";
            }
        }

        /// <summary>
        /// Cluster ID from token
        /// </summary>
        public string IcClusterId
        {
            get
            {
                if (_tokenData == null) return "N/A";
                if (_tokenData.TryGetValue("icClusterId", out var clusterId)) return clusterId;
                return "N/A";
            }
        }

        /// <summary>
        /// Cluster name
        /// </summary>
        public string ClusterName => _cluster?.DisplayName ?? "Unknown";

        /// <summary>
        /// Log content to display
        /// </summary>
        public string LogContent
        {
            get => _logContent;
            set => SetProperty(ref _logContent, value);
        }

        /// <summary>
        /// Current monitoring status
        /// </summary>
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// Whether there are logs to display
        /// </summary>
        public bool HasLogs
        {
            get => _hasLogs;
            set => SetProperty(ref _hasLogs, value);
        }

        /// <summary>
        /// Search text for filtering machines
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    RefreshFilter();
                }
            }
        }

        /// <summary>
        /// Whether regex mode is enabled for search
        /// </summary>
        public bool IsRegexMode
        {
            get => _isRegexMode;
            set
            {
                if (SetProperty(ref _isRegexMode, value))
                {
                    RefreshFilter();
                }
            }
        }

        /// <summary>
        /// Number of machines matching the filter
        /// </summary>
        public int MatchCount
        {
            get => _matchCount;
            private set => SetProperty(ref _matchCount, value);
        }

        /// <summary>
        /// Currently selected machine
        /// </summary>
        public string? SelectedMachine
        {
            get => _selectedMachine;
            set => SetProperty(ref _selectedMachine, value);
        }

        /// <summary>
        /// Filtered collection view of machines
        /// </summary>
        public ICollectionView? FilteredMachines
        {
            get => _filteredMachines;
            private set => SetProperty(ref _filteredMachines, value);
        }

        /// <summary>
        /// Collection of remote machines available for log download
        /// </summary>
        public ObservableCollection<RemoteMachineItem> RemoteMachines
        {
            get => _remoteMachines;
            private set => SetProperty(ref _remoteMachines, value);
        }

        /// <summary>
        /// Currently selected remote machine for log download
        /// </summary>
        public RemoteMachineItem? SelectedRemoteMachine
        {
            get => _selectedRemoteMachine;
            set => SetProperty(ref _selectedRemoteMachine, value);
        }

        #endregion

        #region Machine Filtering

        /// <summary>
        /// Initialize the machine list with sample data
        /// </summary>
        private void InitializeMachineList()
        {
            // Sample machine names - replace with actual data source
            _allMachines.Add("machine-001-prod");
            _allMachines.Add("machine-002-staging");
            _allMachines.Add("machine-003-dev");
            _allMachines.Add("server-alpha-001");
            _allMachines.Add("server-beta-002");
            _allMachines.Add("worker-node-01");
            _allMachines.Add("worker-node-02");
            _allMachines.Add("worker-node-03");
            _allMachines.Add("database-primary");
            _allMachines.Add("database-replica-01");
            _allMachines.Add("cache-redis-001");
            _allMachines.Add("cache-redis-002");
            _allMachines.Add("api-gateway-001");
            _allMachines.Add("api-gateway-002");
            _allMachines.Add("load-balancer-01");
            _allMachines.Add("monitoring-prometheus");
            _allMachines.Add("monitoring-grafana");
            _allMachines.Add("logging-elasticsearch");
            _allMachines.Add("messaging-kafka-001");
            _allMachines.Add("messaging-kafka-002");
        }

        /// <summary>
        /// Initialize remote machines based on cluster name
        /// </summary>
        private void InitializeRemoteMachines()
        {
            // Ensure cluster exists and has a valid name
            if (_cluster == null || string.IsNullOrEmpty(_cluster.Name))
            {
                return;
            }

            // Extract letters and last digit from cluster name
            var clusterName = _cluster.Name;
            var match = Regex.Match(clusterName, @"^([A-Za-z]+)(\d+)$");
            
            if (!match.Success)
            {
                return;
            }

            var letters = match.Groups[1].Value;
            var numbers = match.Groups[2].Value;
            
            // Generate machine names in format: {letters}-C{lastDigit}{type}
            // Example: SC10 -> SC-C0COR01, SC-C0API01, SC-C0WEB01, SC-C0MED01
            
            // COR01 - Virtual Cluster with sub-services
            var cor01 = new RemoteMachineItem
            {
                Name = $"{letters}-C{numbers}COR01",
                DisplayName = $"{letters}-C{numbers}COR01 (Virtual Cluster)",
                IsExpanded = false
            };
            
            // Add COR01 Virtual Cluster services as children
            cor01.Children.Add(new RemoteMachineItem { Name = $"{letters}-C{numbers}COR01-FileServer", DisplayName = "File Server" });
            cor01.Children.Add(new RemoteMachineItem { Name = $"{letters}-C{numbers}COR01-CoOpService", DisplayName = "CoOp Service" });
            cor01.Children.Add(new RemoteMachineItem { Name = $"{letters}-C{numbers}COR01-SurvyService", DisplayName = "Survy Service" });
            cor01.Children.Add(new RemoteMachineItem { Name = $"{letters}-C{numbers}COR01-FSDrivePublisher", DisplayName = "FS Drive Publisher" });
            cor01.Children.Add(new RemoteMachineItem { Name = $"{letters}-C{numbers}COR01-DroneLetter", DisplayName = "Drone Letter" });
            cor01.Children.Add(new RemoteMachineItem { Name = $"{letters}-C{numbers}COR01-DBCWS", DisplayName = "DBCWS" });
            
            _remoteMachines.Add(cor01);
            
            // API01 - Standalone
            _remoteMachines.Add(new RemoteMachineItem 
            { 
                Name = $"{letters}-C{numbers}API01", 
                DisplayName = $"{letters}-C{numbers}API01" 
            });
            
            // WEB01 - Standalone
            _remoteMachines.Add(new RemoteMachineItem 
            { 
                Name = $"{letters}-C{numbers}WEB01", 
                DisplayName = $"{letters}-C{numbers}WEB01" 
            });
            
            // MED01 - Standalone
            _remoteMachines.Add(new RemoteMachineItem 
            { 
                Name = $"{letters}-C{numbers}MED01", 
                DisplayName = $"{letters}-C{numbers}MED01" 
            });
        }

        /// <summary>
        /// Filter predicate for machines
        /// </summary>
        private bool MachineFilter(object item)
        {
            if (item is not string machineName || string.IsNullOrWhiteSpace(SearchText))
                return true;

            if (IsRegexMode)
            {
                try
                {
                    return Regex.IsMatch(machineName, SearchText, RegexOptions.IgnoreCase);
                }
                catch (ArgumentException)
                {
                    // Invalid regex pattern, return false
                    return false;
                }
            }
            else
            {
                return machineName.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Refresh the filter and update match count
        /// </summary>
        private void RefreshFilter()
        {
            _filteredMachines?.Refresh();
            UpdateMatchCount();
        }

        /// <summary>
        /// Update the match count based on filtered items
        /// </summary>
        private void UpdateMatchCount()
        {
            if (_filteredMachines != null)
            {
                int count = 0;
                foreach (var item in _filteredMachines)
                {
                    count++;
                }
                MatchCount = count;
            }
        }

        #endregion
    }
}
