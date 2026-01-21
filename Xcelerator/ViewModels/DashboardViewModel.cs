using System.Windows.Input;
using Xcelerator.Models;
using Xcelerator.NiceClient.Services.Auth;
using Xcelerator.Services;

namespace Xcelerator.ViewModels
{
    /// <summary>
    /// ViewModel for the DashboardView that manages module selection and navigation
    /// </summary>
    public class DashboardViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private readonly PanelViewModel _panelViewModel;
        private readonly Cluster? _cluster;
        private readonly LogFileManager _logFileManager;
        private string _selectedModule = string.Empty;
        private Dictionary<string, string> _tokenData;
        private BaseViewModel? _currentModuleViewModel;
        private readonly HashSet<string> _openModules = new HashSet<string>();

        public DashboardViewModel(MainViewModel mainViewModel, PanelViewModel panelViewModel, LogFileManager logFileManager, Cluster? cluster = null)
        {
            _mainViewModel = mainViewModel;
            _panelViewModel = panelViewModel;
            _cluster = cluster;
            _logFileManager = logFileManager ?? throw new ArgumentNullException(nameof(logFileManager));
            
            SelectModuleCommand = new RelayCommand<string>(SelectModule, CanSelectModule);

            // Decode token if cluster is provided
            if (_cluster != null && !string.IsNullOrEmpty(_cluster.AuthToken))
            {
                _tokenData = JwtDecoder.DecodeToken(_cluster.AuthToken, _cluster.TypeOfCluster);
            }
        }

        /// <summary>
        /// Currently selected module
        /// </summary>
        public string SelectedModule
        {
            get => _selectedModule;
            set => SetProperty(ref _selectedModule, value);
        }

        /// <summary>
        /// Decoded token data
        /// </summary>
        public Dictionary<string, string>? TokenData
        {
            get => _tokenData;
            private set
            {
                if (SetProperty(ref _tokenData, value))
                {
                    // Notify dependent properties when token data changes
                    OnPropertyChanged(nameof(UserName));
                    OnPropertyChanged(nameof(BusinessUnit));
                    OnPropertyChanged(nameof(IcAgentId));
                    OnPropertyChanged(nameof(IcClusterId));
                }
            }
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
        /// Business Unit from token
        /// </summary>
        public string BusinessUnit
        {
            get
            {
                if (_tokenData == null) return "N/A";
                if (_tokenData.TryGetValue("icBUId", out var buId)) return buId;
                return "N/A";
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
        /// Current module view model being displayed
        /// </summary>
        public BaseViewModel? CurrentModuleViewModel
        {
            get => _currentModuleViewModel;
            set => SetProperty(ref _currentModuleViewModel, value);
        }

        public ICommand SelectModuleCommand { get; }

        /// <summary>
        /// Checks if a module can be selected (not already open)
        /// </summary>
        private bool CanSelectModule(string? module)
        {
            if (string.IsNullOrEmpty(module))
                return false;

            // Module can be selected if it's not already in the open modules list
            return !_openModules.Contains(module);
        }

        /// <summary>
        /// Checks if a specific module is already open
        /// </summary>
        public bool IsModuleOpen(string moduleName)
        {
            return _openModules.Contains(moduleName);
        }

        /// <summary>
        /// Select a module
        /// </summary>
        private void SelectModule(string? module)
        {
            if (module != null && !_openModules.Contains(module))
            {
                // Add module to open modules set
                _openModules.Add(module);
                
                SelectedModule = module;
                
                // Store selected module in cluster for persistence
                if (_cluster != null)
                {
                    _cluster.SelectedModule = module;
                }
                
                // Load specific module view into the dashboard content area
                if (module == "LiveLogMonitor")
                {
                    var liveLogMonitorViewModel = new LiveLogMonitorViewModel(_mainViewModel, this, _logFileManager, _cluster, _tokenData);
                    CurrentModuleViewModel = liveLogMonitorViewModel;
                }
                else
                {
                    // Clear module view for other modules (not yet implemented)
                    CurrentModuleViewModel = null;
                }
                
                // Notify that command can execute state has changed for all module buttons
                RaiseCanExecuteChanged();
                
                // Future: Add navigation for other modules
                // else if (module == "ContactForge") { CurrentModuleViewModel = new ContactForgeViewModel(...); }
            }
        }

        /// <summary>
        /// Closes a module and allows it to be selected again
        /// </summary>
        public void CloseModule(string moduleName)
        {
            if (_openModules.Remove(moduleName))
            {
                // If closing the current module, clear the view
                if (SelectedModule == moduleName)
                {
                    SelectedModule = string.Empty;
                    CurrentModuleViewModel = null;
                }
                
                // Notify that command can execute state has changed
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Raises CanExecuteChanged for SelectModuleCommand to update UI button states
        /// </summary>
        private void RaiseCanExecuteChanged()
        {
            if (SelectModuleCommand is RelayCommand<string> relayCommand)
            {
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}
