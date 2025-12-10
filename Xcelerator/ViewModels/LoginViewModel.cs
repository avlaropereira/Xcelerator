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
            
            // Only load existing credentials if the cluster has a valid token
            // This ensures failed authentication attempts don't persist credentials
            if (cluster.HasValidToken)
            {
                AccessKey = cluster.AccessKey ?? string.Empty;
                SecretKey = cluster.SecretKey ?? string.Empty;
            }
            else
            {
                // Start with clean inputs for new or failed authentication
                AccessKey = string.Empty;
                SecretKey = string.Empty;
            }
            
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
                // Note: The AuthService expects tokenUrl, basicAuthHeader, username, and password
                // Use the cluster's Login property as the token URL
                // AccessKey is used as username
                // SecretKey is used as password
                var authToken = await _authService.AuthenticateAsync(
                    tokenUrl: _cluster.Login,
                    basicAuthHeader: "Basic SW50ZXJuYWxAaW5Db250YWN0IEluYy46UVVFNVFrTkdSRE0zUWpFME5FUkRSamczUlVORFJVTkRRakU0TlRrek5UYz0=",
                    username: AccessKey,
                    password: SecretKey
                );

                // Check if authentication was successful
                if (authToken != null && !string.IsNullOrEmpty(authToken.AccessToken))
                {
                    // Store credentials and token in the cluster ONLY on successful authentication
                    _cluster.AccessKey = AccessKey;
                    _cluster.SecretKey = SecretKey;
                    
                    // Store authentication token information
                    _cluster.AuthToken = authToken.AccessToken;
                    _cluster.TokenType = authToken.TokenType;
                    _cluster.RefreshToken = authToken.RefreshToken;
                    _cluster.ResourceServerBaseUri = authToken.ResourceServerBaseUri;
                    
                    // Calculate token expiration time (current time + expires_in seconds)
                    if (authToken.ExpiresIn > 0)
                    {
                        _cluster.TokenExpirationTime = DateTime.UtcNow.AddSeconds(authToken.ExpiresIn);
                    }

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
                    // Clear invalid credentials and token from cluster
                    ClearClusterAuthentication();
                    
                    // Clear the input fields as well
                    AccessKey = string.Empty;
                    SecretKey = string.Empty;
                    
                    ErrorMessage = "Authentication failed. The credentials provided are not valid.";
                }
            }
            catch (HttpRequestException ex)
            {
                // Clear invalid credentials and token from cluster
                ClearClusterAuthentication();
                
                // Clear the input fields as well
                AccessKey = string.Empty;
                SecretKey = string.Empty;
                
                // Handle authentication failure
                ErrorMessage = "Authentication failed. The credentials provided are not valid. Please check your Access Key and Secret Key.";
                
                // Optionally log the exception for debugging
                System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Clear credentials and token from cluster on unexpected errors
                ClearClusterAuthentication();
                
                // Clear the input fields as well
                AccessKey = string.Empty;
                SecretKey = string.Empty;
                
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

        /// <summary>
        /// Clears all authentication data from the cluster
        /// </summary>
        private void ClearClusterAuthentication()
        {
            _cluster.AccessKey = string.Empty;
            _cluster.SecretKey = string.Empty;
            _cluster.AuthToken = string.Empty;
            _cluster.TokenType = string.Empty;
            _cluster.RefreshToken = string.Empty;
            _cluster.ResourceServerBaseUri = string.Empty;
            _cluster.TokenExpirationTime = null;
        }
    }
}
