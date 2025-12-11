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
        private readonly Cluster? _cluster;
        private string _selectedModule = string.Empty;
        private UserTokenPayload? _tokenData;

        public DashboardViewModel(MainViewModel mainViewModel, Cluster? cluster = null)
        {
            _mainViewModel = mainViewModel;
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
                
                // Here you would typically navigate to the specific module page
                // For now, we'll just update the selected module
            }
        }
    }
}
