using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows.Data;
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
        private string _searchText = string.Empty;
        private int _matchCount = 0;
        private ICollectionView? _filteredClusters;
        
        // Track downloaded log files per cluster for cleanup
        private static readonly Dictionary<string, HashSet<string>> _clusterLogFiles = new Dictionary<string, HashSet<string>>();

        public PanelViewModel(MainViewModel mainViewModel, IAuthService authService)
        {
            _mainViewModel = mainViewModel;
            _authService = authService;
            _availableClusters = new ObservableCollection<Cluster>();
            _selectedClusters = new ObservableCollection<Cluster>();

            InitializeClusters();
            LoadSelectedClusters();

            // Setup filtered collection view
            _filteredClusters = CollectionViewSource.GetDefaultView(_availableClusters);
            if (_filteredClusters != null)
            {
                _filteredClusters.Filter = ClusterFilter;
            }
            UpdateMatchCount();

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

        /// <summary>
        /// Search text for filtering clusters
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
        /// Number of clusters matching the filter
        /// </summary>
        public int MatchCount
        {
            get => _matchCount;
            private set => SetProperty(ref _matchCount, value);
        }

        /// <summary>
        /// Filtered collection view of clusters
        /// </summary>
        public ICollectionView? FilteredClusters
        {
            get => _filteredClusters;
            private set => SetProperty(ref _filteredClusters, value);
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
            
            try
            {
                string clusterJsonPath = @"C:\XceleratorTool\Resources\cluster.json";
                
                if (File.Exists(clusterJsonPath))
                {
                    string jsonContent = File.ReadAllText(clusterJsonPath);
                    var clusterConfigs = JsonSerializer.Deserialize<List<ClusterConfig>>(jsonContent);
                    
                    if (clusterConfigs != null)
                    {
                        foreach (var config in clusterConfigs)
                        {
                            var cluster = new Cluster(config.Name, config.Name)
                            {
                                ApiBaseURL = config.ApiBaseURL,
                                Login = config.Login,
                                TypeOfCluster = config.TypeOfCluster
                            };
                            AvailableClusters.Add(cluster);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error and use fallback data
                System.Diagnostics.Debug.WriteLine($"Error loading clusters from JSON: {ex.Message}");
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
        /// Navigate directly to Dashboard (skip login)
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

                // Navigate directly to Dashboard when cluster is selected
                SelectedClusterForLogin = cluster;
                cluster.IsInDashboardMode = true;
                NavigateToDashboard();
            }
        }

        /// <summary>
        /// Deselect a cluster and remove it from the selected clusters list
        /// Also cleans up all downloaded log files associated with this cluster
        /// </summary>
        private void DeselectCluster(Cluster? cluster)
        {
            if (cluster == null) return;

            // Clean up all downloaded log files for this cluster
            CleanupClusterLogFiles(cluster.Name);

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
        /// Handle cluster tag click - navigate directly to dashboard (skip login)
        /// </summary>
        private void TagClick(Cluster? cluster)
        {
            if (cluster == null) return;

            SelectedClusterForLogin = cluster;

            // Always navigate directly to Dashboard when clicking on selected cluster
            // Skip login regardless of token state
            _mainViewModel.Credentials.AccessKey = cluster.AccessKey;
            _mainViewModel.Credentials.SecretKey = cluster.SecretKey;
            cluster.IsInDashboardMode = true;
            NavigateToDashboard();
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
                var dashboardViewModel = new DashboardViewModel(_mainViewModel, this, SelectedClusterForLogin)
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

        #region Cluster Filtering

        /// <summary>
        /// Filter predicate for clusters
        /// </summary>
        private bool ClusterFilter(object item)
        {
            if (item is not Cluster cluster || string.IsNullOrWhiteSpace(SearchText))
                return true;

            return cluster.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Refresh the filter and update match count
        /// </summary>
        private void RefreshFilter()
        {
            _filteredClusters?.Refresh();
            UpdateMatchCount();
        }

        /// <summary>
        /// Update the match count based on filtered items
        /// </summary>
        private void UpdateMatchCount()
        {
            if (_filteredClusters != null)
            {
                int count = 0;
                foreach (var item in _filteredClusters)
                {
                    count++;
                }
                MatchCount = count;
            }
        }

        #endregion

        #region Log File Management

        /// <summary>
        /// Registers a log file path for a specific cluster
        /// This allows tracking and cleanup when the cluster is deselected
        /// </summary>
        public static void RegisterLogFile(string clusterName, string logFilePath)
        {
            if (string.IsNullOrEmpty(clusterName) || string.IsNullOrEmpty(logFilePath))
                return;

            lock (_clusterLogFiles)
            {
                if (!_clusterLogFiles.ContainsKey(clusterName))
                {
                    _clusterLogFiles[clusterName] = new HashSet<string>();
                }
                _clusterLogFiles[clusterName].Add(logFilePath);
            }
        }

        /// <summary>
        /// Cleans up all downloaded log files for a specific cluster
        /// Removes both individual files and their parent directories if empty
        /// </summary>
        private void CleanupClusterLogFiles(string clusterName)
        {
            if (string.IsNullOrEmpty(clusterName))
                return;

            HashSet<string>? logFiles = null;

            lock (_clusterLogFiles)
            {
                if (_clusterLogFiles.ContainsKey(clusterName))
                {
                    logFiles = new HashSet<string>(_clusterLogFiles[clusterName]);
                    _clusterLogFiles.Remove(clusterName);
                }
            }

            if (logFiles == null || logFiles.Count == 0)
                return;

            int filesDeleted = 0;
            int dirsDeleted = 0;
            var directories = new HashSet<string>();

            // Delete all log files
            foreach (var logFile in logFiles)
            {
                try
                {
                    if (File.Exists(logFile))
                    {
                        File.Delete(logFile);
                        filesDeleted++;

                        // Track parent directory for cleanup
                        var directory = Path.GetDirectoryName(logFile);
                        if (!string.IsNullOrEmpty(directory))
                        {
                            directories.Add(directory);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deleting log file {logFile}: {ex.Message}");
                }
            }

            // Delete empty directories
            foreach (var directory in directories)
            {
                try
                {
                    if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                    {
                        Directory.Delete(directory);
                        dirsDeleted++;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deleting directory {directory}: {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"Cleaned up cluster '{clusterName}': {filesDeleted} files and {dirsDeleted} directories deleted.");
        }

        #endregion
    }
}
