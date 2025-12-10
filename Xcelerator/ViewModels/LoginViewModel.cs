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
        private string _errorMessage = string.Empty;

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
                ClearErrorMessage();
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
                ClearErrorMessage();
            }
        }

        /// <summary>
        /// Error message to display validation errors
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
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
            if (!ValidateCredentials())
            {
                return;
            }

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

        /// <summary>
        /// Check if sign in is possible
        /// </summary>
        private bool CanSignIn()
        {
            return !string.IsNullOrWhiteSpace(AccessKey) && !string.IsNullOrWhiteSpace(SecretKey);
        }

        /// <summary>
        /// Validates credentials and sets error message if validation fails
        /// </summary>
        private bool ValidateCredentials()
        {
            if (string.IsNullOrWhiteSpace(AccessKey) && string.IsNullOrWhiteSpace(SecretKey))
            {
                ErrorMessage = "Access Key and Secret Key are required. Please provide both credentials.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(AccessKey))
            {
                ErrorMessage = "Access Key is required. Please provide your Access Key.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(SecretKey))
            {
                ErrorMessage = "Secret Key is required. Please provide your Secret Key.";
                return false;
            }

            ClearErrorMessage();
            return true;
        }

        /// <summary>
        /// Clears the error message
        /// </summary>
        private void ClearErrorMessage()
        {
            ErrorMessage = string.Empty;
        }
    }
}
