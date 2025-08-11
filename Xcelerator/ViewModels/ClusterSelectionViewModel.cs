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
    }
}
