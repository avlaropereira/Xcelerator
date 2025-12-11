using System.Windows.Input;
using Xcelerator.Models;
using Xcelerator.NiceClient.Models;
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
        private UserTokenPayload? _tokenData;
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
                _tokenData = JwtDecoder.DecodeToken(_cluster.AuthToken);
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
        public UserTokenPayload? TokenData
        {
            get => _tokenData;
            private set => SetProperty(ref _tokenData, value);
        }

        /// <summary>
        /// User name from token
        /// </summary>
        public string UserName => _tokenData?.Name ?? _tokenData?.GivenName ?? "User";

        /// <summary>
        /// Business Unit from token
        /// </summary>
        public string BusinessUnit => _tokenData?.IcBUId.ToString() ?? "N/A";

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
                    var liveLogMonitorViewModel = new LiveLogMonitorViewModel(_mainViewModel, this, _cluster);
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
