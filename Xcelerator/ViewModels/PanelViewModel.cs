using System.Collections.ObjectModel;
using System.Windows.Input;
using Xcelerator.Models;
using Xcelerator.NiceClient.Services.Auth;

namespace Xcelerator.ViewModels
{
    /// <summary>
    /// ViewModel for the PanelView that manages cluster selection and dynamic view switching
    /// </summary>
    public class PanelViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IAuthService _authService;
        private ObservableCollection<Cluster> _availableClusters;
        private ObservableCollection<Cluster> _selectedClusters;
        private BaseViewModel? _currentViewModel;
        private Cluster? _selectedClusterForLogin;

        public PanelViewModel(MainViewModel mainViewModel, IAuthService authService)
        {
            _mainViewModel = mainViewModel;
            _authService = authService;
            _availableClusters = new ObservableCollection<Cluster>();
            _selectedClusters = new ObservableCollection<Cluster>();

            InitializeClusters();
            LoadSelectedClusters();

            // Initialize commands
            SelectClusterCommand = new RelayCommand<Cluster>(SelectCluster);
            DeselectClusterCommand = new RelayCommand<Cluster>(DeselectCluster);
            TagClickCommand = new RelayCommand<Cluster>(TagClick);
        }

        #region Properties

        /// <summary>
        /// Available clusters for selection
        /// </summary>
        public ObservableCollection<Cluster> AvailableClusters
        {
            get => _availableClusters;
            set => SetProperty(ref _availableClusters, value);
        }

        /// <summary>
        /// Currently selected clusters
        /// </summary>
        public ObservableCollection<Cluster> SelectedClusters
        {
            get => _selectedClusters;
            set => SetProperty(ref _selectedClusters, value);
        }

        /// <summary>
        /// Currently displayed view model (for dynamic view switching)
        /// </summary>
        public BaseViewModel? CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        /// <summary>
        /// Cluster selected for login operations
        /// </summary>
        public Cluster? SelectedClusterForLogin
        {
            get => _selectedClusterForLogin;
            set => SetProperty(ref _selectedClusterForLogin, value);
        }

        #endregion

        #region Commands

        public ICommand SelectClusterCommand { get; }
        public ICommand DeselectClusterCommand { get; }
        public ICommand TagClickCommand { get; }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize available clusters with sample data
        /// </summary>
        private void InitializeClusters()
        {
            AvailableClusters.Clear();
            // Add sample clusters
            for (int i = 1; i <= 20; i++)
            {
                AvailableClusters.Add(new Cluster($"sc{i}", $"SC{i}"));
            }
        }

        /// <summary>
        /// Load previously selected clusters from main view model
        /// </summary>
        private void LoadSelectedClusters()
        {
            SelectedClusters.Clear();
            foreach (var cluster in _mainViewModel.Credentials.SelectedClusters)
            {
                SelectedClusters.Add(cluster);
            }
        }

        /// <summary>
        /// Select a cluster and add it to the selected clusters list
        /// </summary>
        private void SelectCluster(Cluster? cluster)
        {
            if (cluster == null) return;

            if (!SelectedClusters.Any(c => c.Name == cluster.Name))
            {
                // Ensure the cluster starts completely clean when first selected
                cluster.AccessKey = string.Empty;
                cluster.SecretKey = string.Empty;
                cluster.AuthToken = string.Empty;
                cluster.TokenType = string.Empty;
                cluster.RefreshToken = string.Empty;
                cluster.ResourceServerBaseUri = string.Empty;
                cluster.TokenExpirationTime = null;
                cluster.SelectedModule = string.Empty;
                cluster.IsInDashboardMode = false;
                
                cluster.IsSelected = true;
                SelectedClusters.Add(cluster);
                _mainViewModel.Credentials.SelectedClusters.Add(cluster);
            }
        }

        /// <summary>
        /// Deselect a cluster and remove it from the selected clusters list
        /// </summary>
        private void DeselectCluster(Cluster? cluster)
        {
            if (cluster == null) return;

            // Clear stored credentials, token, and dashboard state for this cluster
            cluster.AccessKey = string.Empty;
            cluster.SecretKey = string.Empty;
            cluster.AuthToken = string.Empty;
            cluster.TokenType = string.Empty;
            cluster.RefreshToken = string.Empty;
            cluster.ResourceServerBaseUri = string.Empty;
            cluster.TokenExpirationTime = null;
            cluster.SelectedModule = string.Empty;
            cluster.IsInDashboardMode = false;

            // If this cluster was selected for login, clear the current view
            if (SelectedClusterForLogin?.Name == cluster.Name)
            {
                SelectedClusterForLogin = null;
                CurrentViewModel = null;
            }

            cluster.IsSelected = false;
            SelectedClusters.Remove(cluster);
            _mainViewModel.Credentials.SelectedClusters.Remove(cluster);
        }

        /// <summary>
        /// Handle cluster tag click - shows login form or dashboard based on cluster state
        /// </summary>
        private void TagClick(Cluster? cluster)
        {
            if (cluster == null) return;

            SelectedClusterForLogin = cluster;

            // If cluster has valid token, show dashboard mode
            // Otherwise show login form even if it has credentials (token might be expired)
            if (cluster.HasValidToken)
            {
                _mainViewModel.Credentials.AccessKey = cluster.AccessKey;
                _mainViewModel.Credentials.SecretKey = cluster.SecretKey;
                cluster.IsInDashboardMode = true;
                NavigateToDashboard();
            }
            else
            {
                // Show login form for this specific cluster
                cluster.IsInDashboardMode = false;
                NavigateToLogin();
            }
        }

        /// <summary>
        /// Handle login completion - switch to dashboard after successful login
        /// </summary>
        public void OnLoginCompleted(Cluster cluster)
        {
            if (cluster.HasValidToken)
            {
                _mainViewModel.Credentials.AccessKey = cluster.AccessKey;
                _mainViewModel.Credentials.SecretKey = cluster.SecretKey;
                cluster.IsInDashboardMode = true;
                NavigateToDashboard();
            }
        }

        /// <summary>
        /// Navigate to dashboard view
        /// </summary>
        private void NavigateToDashboard()
        {
            if (SelectedClusterForLogin != null)
            {
                var dashboardViewModel = new DashboardViewModel(_mainViewModel)
                {
                    SelectedModule = SelectedClusterForLogin.SelectedModule
                };
                CurrentViewModel = dashboardViewModel;
            }
        }

        /// <summary>
        /// Navigate to login view
        /// </summary>
        private void NavigateToLogin()
        {
            if (SelectedClusterForLogin != null)
            {
                var loginViewModel = new LoginViewModel(_mainViewModel, SelectedClusterForLogin, _authService);
                CurrentViewModel = loginViewModel;
            }
        }

        #endregion
    }
}
