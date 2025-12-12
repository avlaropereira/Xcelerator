using System.Windows.Input;
using Xcelerator.Models;
using Xcelerator.NiceClient.Services.Auth;

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
        private string _selectedModule = string.Empty;
        private Dictionary<string, string> _tokenData;
        private BaseViewModel? _currentModuleViewModel;

        public DashboardViewModel(MainViewModel mainViewModel, PanelViewModel panelViewModel, Cluster? cluster = null)
        {
            _mainViewModel = mainViewModel;
            _panelViewModel = panelViewModel;
            _cluster = cluster;
            
            SelectModuleCommand = new RelayCommand<string>(SelectModule);

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
        /// Select a module
        /// </summary>
        private void SelectModule(string? module)
        {
            if (module != null)
            {
                SelectedModule = module;
                
                // Store selected module in cluster for persistence
                if (_cluster != null)
                {
                    _cluster.SelectedModule = module;
                }
                
                // Load specific module view into the dashboard content area
                if (module == "LiveLogMonitor")
                {
                    var liveLogMonitorViewModel = new LiveLogMonitorViewModel(_mainViewModel, this, _cluster, _tokenData);
                    CurrentModuleViewModel = liveLogMonitorViewModel;
                }
                else
                {
                    // Clear module view for other modules (not yet implemented)
                    CurrentModuleViewModel = null;
                }
                // Future: Add navigation for other modules
                // else if (module == "ContactForge") { CurrentModuleViewModel = new ContactForgeViewModel(...); }
            }
        }
    }
}
