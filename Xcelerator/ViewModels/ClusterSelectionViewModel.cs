using System.Collections.ObjectModel;
using System.Windows.Input;
using Xcelerator.Models;

namespace Xcelerator.ViewModels
{
    public class ClusterSelectionViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private ObservableCollection<Cluster> _availableClusters;
        private ObservableCollection<Cluster> _selectedClusters;
        private Cluster? _selectedClusterForLogin;
        private string _accessKey = string.Empty;
        private string _secretKey = string.Empty;

        public ClusterSelectionViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _availableClusters = new ObservableCollection<Cluster>();
            _selectedClusters = new ObservableCollection<Cluster>();

            InitializeClusters();
            LoadSelectedClusters();

            SelectClusterCommand = new RelayCommand<Cluster>(SelectCluster);
            DeselectClusterCommand = new RelayCommand<Cluster>(DeselectCluster);
            TagClickCommand = new RelayCommand<Cluster>(TagClick);
            SignInCommand = new RelayCommand(SignIn, CanSignIn);
            GoBackFromDashboardCommand = new RelayCommand(GoBackFromDashboard);
            SelectModuleCommand = new RelayCommand<string>(SelectModule);
        }

        public ObservableCollection<Cluster> AvailableClusters
        {
            get => _availableClusters;
            set => SetProperty(ref _availableClusters, value);
        }

        public ObservableCollection<Cluster> SelectedClusters
        {
            get => _selectedClusters;
            set => SetProperty(ref _selectedClusters, value);
        }

        public ICommand SelectClusterCommand { get; }
        public ICommand DeselectClusterCommand { get; }
        public ICommand TagClickCommand { get; }
        public ICommand SignInCommand { get; }
        public ICommand GoBackFromDashboardCommand { get; }
        public ICommand SelectModuleCommand { get; }

        public Cluster? SelectedClusterForLogin
        {
            get => _selectedClusterForLogin;
            set 
            { 
                SetProperty(ref _selectedClusterForLogin, value);
                OnPropertyChanged(nameof(ShowLoginForm));
            }
        }

        public string AccessKey
        {
            get => _accessKey;
            set => SetProperty(ref _accessKey, value);
        }

        public string SecretKey
        {
            get => _secretKey;
            set => SetProperty(ref _secretKey, value);
        }

        public bool IsInDashboardMode
        {
            get => SelectedClusterForLogin?.IsInDashboardMode ?? false;
        }

        public bool ShowLoginForm 
        { 
            get => SelectedClusterForLogin != null && !IsInDashboardMode; 
        }

        public string SelectedModule
        {
            get => SelectedClusterForLogin?.SelectedModule ?? string.Empty;
            set
            {
                if (SelectedClusterForLogin != null)
                {
                    SelectedClusterForLogin.SelectedModule = value;
                    OnPropertyChanged();
                }
            }
        }

        private void InitializeClusters()
        {
            AvailableClusters.Clear();
            // Add sample clusters
            for (int i = 1; i <= 20; i++)
            {
                AvailableClusters.Add(new Cluster($"sc{i}", $"SC{i}"));
            }
        }

        private void LoadSelectedClusters()
        {
            SelectedClusters.Clear();
            foreach (var cluster in _mainViewModel.Credentials.SelectedClusters)
            {
                SelectedClusters.Add(cluster);
            }
        }

        private void SelectCluster(Cluster? cluster)
        {
            if (cluster == null) return;

            if (!SelectedClusters.Any(c => c.Name == cluster.Name))
            {
                // Ensure the cluster has no existing credentials when first selected
                cluster.AccessKey = string.Empty;
                cluster.SecretKey = string.Empty;
                
                cluster.IsSelected = true;
                SelectedClusters.Add(cluster);
                _mainViewModel.Credentials.SelectedClusters.Add(cluster);
            }
        }

        private void DeselectCluster(Cluster? cluster)
        {
            if (cluster == null) return;

            // Clear stored credentials and dashboard state for this cluster
            cluster.AccessKey = string.Empty;
            cluster.SecretKey = string.Empty;
            cluster.SelectedModule = string.Empty;
            cluster.IsInDashboardMode = false;

            // If this cluster was selected for login, clear the login form
            if (SelectedClusterForLogin?.Name == cluster.Name)
            {
                SelectedClusterForLogin = null;
                AccessKey = string.Empty;
                SecretKey = string.Empty;
                OnPropertyChanged(nameof(IsInDashboardMode));
                OnPropertyChanged(nameof(ShowLoginForm));
            }

            cluster.IsSelected = false;
            SelectedClusters.Remove(cluster);
            _mainViewModel.Credentials.SelectedClusters.Remove(cluster);
        }



        private void TagClick(Cluster? cluster)
        {
            if (cluster == null) return;

            // If cluster has credentials, show dashboard mode
            if (cluster.HasCredentials)
            {
                _mainViewModel.Credentials.AccessKey = cluster.AccessKey;
                _mainViewModel.Credentials.SecretKey = cluster.SecretKey;
                cluster.IsInDashboardMode = true;
                SelectedClusterForLogin = cluster;
                OnPropertyChanged(nameof(IsInDashboardMode));
                OnPropertyChanged(nameof(ShowLoginForm));
            }
            else
            {
                // Show login form for this specific cluster
                // Clear any previous login form data to ensure isolation
                if (SelectedClusterForLogin != null && SelectedClusterForLogin.Name != cluster.Name)
                {
                    // Save any unsaved credentials from the previous cluster
                    SavePartialCredentials();
                }
                
                SelectedClusterForLogin = cluster;
                cluster.IsInDashboardMode = false;
                OnPropertyChanged(nameof(IsInDashboardMode));
                OnPropertyChanged(nameof(ShowLoginForm));
                
                // Load existing credentials for this cluster if they exist, otherwise clear
                if (cluster.HasCredentials)
                {
                    AccessKey = cluster.AccessKey;
                    SecretKey = cluster.SecretKey;
                }
                else
                {
                    AccessKey = string.Empty;
                    SecretKey = string.Empty;
                }
            }
        }

        private void SignIn()
        {
            if (SelectedClusterForLogin != null && CanSignIn())
            {
                // Store credentials for this specific cluster
                SelectedClusterForLogin.AccessKey = AccessKey;
                SelectedClusterForLogin.SecretKey = SecretKey;

                // Set credentials for main view model
                _mainViewModel.Credentials.AccessKey = AccessKey;
                _mainViewModel.Credentials.SecretKey = SecretKey;

                // Clear the login form and switch to dashboard mode
                AccessKey = string.Empty;
                SecretKey = string.Empty;
                SelectedClusterForLogin.IsInDashboardMode = true;
                OnPropertyChanged(nameof(IsInDashboardMode));
                OnPropertyChanged(nameof(ShowLoginForm));
            }
        }

        private bool CanSignIn()
        {
            return !string.IsNullOrWhiteSpace(AccessKey) && !string.IsNullOrWhiteSpace(SecretKey);
        }

        // Method to save partial credentials when switching clusters
        private void SavePartialCredentials()
        {
            if (SelectedClusterForLogin != null && (!string.IsNullOrWhiteSpace(AccessKey) || !string.IsNullOrWhiteSpace(SecretKey)))
            {
                SelectedClusterForLogin.AccessKey = AccessKey;
                SelectedClusterForLogin.SecretKey = SecretKey;
            }
        }

        private void GoBackFromDashboard()
        {
            if (SelectedClusterForLogin != null)
            {
                SelectedClusterForLogin.IsInDashboardMode = false;
            }
            SelectedClusterForLogin = null;
            OnPropertyChanged(nameof(IsInDashboardMode));
            OnPropertyChanged(nameof(ShowLoginForm));
        }

        private void SelectModule(string? moduleName)
        {
            if (!string.IsNullOrEmpty(moduleName) && SelectedClusterForLogin != null)
            {
                SelectedClusterForLogin.SelectedModule = moduleName;
                OnPropertyChanged(nameof(SelectedModule));
            }
        }
    }
}
