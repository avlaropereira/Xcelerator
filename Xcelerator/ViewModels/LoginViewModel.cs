using System.Windows.Input;
using Xcelerator.Models;

namespace Xcelerator.ViewModels
{
    /// <summary>
    /// ViewModel for the LoginView that handles authentication for a specific cluster
    /// </summary>
    public class LoginViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private readonly Cluster _cluster;
        private string _accessKey = string.Empty;
        private string _secretKey = string.Empty;

        public LoginViewModel(MainViewModel mainViewModel, Cluster cluster)
        {
            _mainViewModel = mainViewModel;
            _cluster = cluster;
            
            // Load existing credentials if available
            AccessKey = cluster.AccessKey ?? string.Empty;
            SecretKey = cluster.SecretKey ?? string.Empty;
            
            SignInCommand = new RelayCommand(SignIn, CanSignIn);
        }

        /// <summary>
        /// Access key for authentication
        /// </summary>
        public string AccessKey
        {
            get => _accessKey;
            set
            {
                SetProperty(ref _accessKey, value);
                _cluster.AccessKey = value;
            }
        }

        /// <summary>
        /// Secret key for authentication
        /// </summary>
        public string SecretKey
        {
            get => _secretKey;
            set
            {
                SetProperty(ref _secretKey, value);
                _cluster.SecretKey = value;
            }
        }

        /// <summary>
        /// Display name of the cluster being authenticated
        /// </summary>
        public string ClusterDisplayName => _cluster.DisplayName;

        public ICommand SignInCommand { get; }

        /// <summary>
        /// Handle sign in process
        /// </summary>
        private void SignIn()
        {
            if (CanSignIn())
            {
                // Store credentials in the cluster
                _cluster.AccessKey = AccessKey;
                _cluster.SecretKey = SecretKey;

                // Set credentials for main view model
                _mainViewModel.Credentials.AccessKey = AccessKey;
                _mainViewModel.Credentials.SecretKey = SecretKey;

                // Switch to dashboard mode
                _cluster.IsInDashboardMode = true;
                
                // Notify the parent PanelViewModel to switch to dashboard
                if (_mainViewModel.CurrentPage is PanelViewModel panelViewModel)
                {
                    panelViewModel.OnLoginCompleted(_cluster);
                }
            }
        }

        /// <summary>
        /// Check if sign in is possible
        /// </summary>
        private bool CanSignIn()
        {
            return !string.IsNullOrWhiteSpace(AccessKey) && !string.IsNullOrWhiteSpace(SecretKey);
        }
    }
}
