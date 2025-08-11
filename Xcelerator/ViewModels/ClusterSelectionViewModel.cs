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
            ContinueCommand = new RelayCommand(Continue, CanContinue);
            TagClickCommand = new RelayCommand<Cluster>(TagClick);
            SignInCommand = new RelayCommand(SignIn, CanSignIn);
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
        public ICommand ContinueCommand { get; }
        public ICommand TagClickCommand { get; }
        public ICommand SignInCommand { get; }

        public Cluster? SelectedClusterForLogin
        {
            get => _selectedClusterForLogin;
            set => SetProperty(ref _selectedClusterForLogin, value);
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
                cluster.IsSelected = true;
                SelectedClusters.Add(cluster);
                _mainViewModel.Credentials.SelectedClusters.Add(cluster);
            }
        }

        private void DeselectCluster(Cluster? cluster)
        {
            if (cluster == null) return;

            cluster.IsSelected = false;
            SelectedClusters.Remove(cluster);
            _mainViewModel.Credentials.SelectedClusters.Remove(cluster);
        }

        private void Continue()
        {
            _mainViewModel.NavigateToLoginCommand.Execute(null);
        }

        private bool CanContinue()
        {
            return SelectedClusters.Any();
        }

        private void TagClick(Cluster? cluster)
        {
            if (cluster == null) return;

            // If cluster has credentials, navigate directly to dashboard
            if (cluster.HasCredentials)
            {
                _mainViewModel.Credentials.AccessKey = cluster.AccessKey;
                _mainViewModel.Credentials.SecretKey = cluster.SecretKey;
                _mainViewModel.NavigateToDashboardCommand.Execute(null);
            }
            else
            {
                // Show login form for this specific cluster
                SelectedClusterForLogin = cluster;
                AccessKey = string.Empty;
                SecretKey = string.Empty;
            }
        }

        private void SignIn()
        {
            if (SelectedClusterForLogin != null && CanSignIn())
            {
                // Store credentials for this cluster
                SelectedClusterForLogin.AccessKey = AccessKey;
                SelectedClusterForLogin.SecretKey = SecretKey;

                // Set credentials for main view model
                _mainViewModel.Credentials.AccessKey = AccessKey;
                _mainViewModel.Credentials.SecretKey = SecretKey;

                // Navigate to dashboard
                _mainViewModel.NavigateToDashboardCommand.Execute(null);
            }
        }

        private bool CanSignIn()
        {
            return !string.IsNullOrWhiteSpace(AccessKey) && !string.IsNullOrWhiteSpace(SecretKey);
        }
    }
}
