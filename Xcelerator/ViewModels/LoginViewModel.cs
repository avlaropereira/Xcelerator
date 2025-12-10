using System.Net.Http;
using System.Windows.Input;
using Xcelerator.Models;
using Xcelerator.NiceClient.Services.Auth;

namespace Xcelerator.ViewModels
{
    /// <summary>
    /// ViewModel for the LoginView that handles authentication for a specific cluster
    /// </summary>
    public class LoginViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private readonly Cluster _cluster;
        private readonly IAuthService _authService;
        private string _accessKey = string.Empty;
        private string _secretKey = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isAuthenticating = false;

        public LoginViewModel(MainViewModel mainViewModel, Cluster cluster, IAuthService authService)
        {
            _mainViewModel = mainViewModel;
            _cluster = cluster;
            _authService = authService;
            
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
        /// Indicates if authentication is in progress
        /// </summary>
        public bool IsAuthenticating
        {
            get => _isAuthenticating;
            set => SetProperty(ref _isAuthenticating, value);
        }

        /// <summary>
        /// Display name of the cluster being authenticated
        /// </summary>
        public string ClusterDisplayName => _cluster.DisplayName;

        public ICommand SignInCommand { get; }

        /// <summary>
        /// Handle sign in process
        /// </summary>
        private async void SignIn()
        {
            if (!ValidateCredentials())
            {
                return;
            }

            IsAuthenticating = true;
            ClearErrorMessage();

            try
            {
                // Authenticate using the AuthService
                // Note: The AuthService expects basicAuthHeader, username, and password
                // AccessKey is used as basicAuthHeader (Base64 encoded credentials)
                // SecretKey is used as password
                var authToken = await _authService.AuthenticateAsync(
                    basicAuthHeader: "Basic SW50ZXJuYWxAaW5Db250YWN0IEluYy46UVVFNVFrTkdSRE0zUWpFME5FUkRSamczUlVORFJVTkRRakU0TlRrek5UYz0=",
                    username: AccessKey,
                    password: SecretKey
                );

                // Check if authentication was successful
                if (authToken != null && !string.IsNullOrEmpty(authToken.AccessToken))
                {
                    // Store credentials and token in the cluster
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
                else
                {
                    ErrorMessage = "Authentication failed. The credentials provided are not valid.";
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle authentication failure
                ErrorMessage = "Authentication failed. The credentials provided are not valid. Please check your Access Key and Secret Key.";
                
                // Optionally log the exception for debugging
                System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                ErrorMessage = "An unexpected error occurred during authentication. Please try again.";
                
                // Optionally log the exception for debugging
                System.Diagnostics.Debug.WriteLine($"Unexpected error: {ex.Message}");
            }
            finally
            {
                IsAuthenticating = false;
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
