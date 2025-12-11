using System.Windows.Input;
using Xcelerator.Models;
using Xcelerator.NiceClient.Models;
using Xcelerator.NiceClient.Services.Auth;

namespace Xcelerator.ViewModels
{
    /// <summary>
    /// ViewModel for the LiveLogMonitorView that manages log monitoring functionality
    /// </summary>
    public class LiveLogMonitorViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private readonly DashboardViewModel _dashboardViewModel;
        private readonly Cluster? _cluster;
        private UserTokenPayload? _tokenData;
        private string _logContent = string.Empty;
        private string _status = "Idle";
        private bool _isMonitoring = false;
        private bool _hasLogs = false;

        public LiveLogMonitorViewModel(MainViewModel mainViewModel, DashboardViewModel dashboardViewModel, Cluster? cluster = null)
        {
            _mainViewModel = mainViewModel;
            _dashboardViewModel = dashboardViewModel;
            _cluster = cluster;

            // Initialize commands
            BackToDashboardCommand = new RelayCommand(BackToDashboard);
            StartMonitoringCommand = new RelayCommand(StartMonitoring, CanStartMonitoring);
            StopMonitoringCommand = new RelayCommand(StopMonitoring, CanStopMonitoring);

            // Decode token if cluster is provided
            if (_cluster != null && !string.IsNullOrEmpty(_cluster.AuthToken))
            {
                _tokenData = JwtDecoder.DecodeToken(_cluster.AuthToken);
            }

            // Set initial status
            Status = "Ready";
        }

        #region Properties

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
        /// Cluster name
        /// </summary>
        public string ClusterName => _cluster?.DisplayName ?? "Unknown";

        /// <summary>
        /// Log content to display
        /// </summary>
        public string LogContent
        {
            get => _logContent;
            set => SetProperty(ref _logContent, value);
        }

        /// <summary>
        /// Current monitoring status
        /// </summary>
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// Whether monitoring is currently active
        /// </summary>
        public bool IsMonitoring
        {
            get => _isMonitoring;
            set
            {
                if (SetProperty(ref _isMonitoring, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// Whether there are logs to display
        /// </summary>
        public bool HasLogs
        {
            get => _hasLogs;
            set => SetProperty(ref _hasLogs, value);
        }

        #endregion

        #region Commands

        public ICommand BackToDashboardCommand { get; }
        public ICommand StartMonitoringCommand { get; }
        public ICommand StopMonitoringCommand { get; }

        #endregion

        #region Command Methods

        /// <summary>
        /// Navigate back to the dashboard
        /// </summary>
        private void BackToDashboard()
        {
            // Stop monitoring if active
            if (IsMonitoring)
            {
                StopMonitoring();
            }

            // Clear the module view to return to dashboard home
            if (_cluster != null)
            {
                _cluster.SelectedModule = string.Empty;
            }
            _dashboardViewModel.SelectedModule = string.Empty;
            _dashboardViewModel.CurrentModuleViewModel = null;
        }

        /// <summary>
        /// Start log monitoring
        /// </summary>
        private void StartMonitoring()
        {
            IsMonitoring = true;
            Status = "Monitoring...";
            HasLogs = true;

            // Add initial log entry
            LogContent = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Log monitoring started for cluster: {ClusterName}\n";
            LogContent += $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Waiting for log events...\n";

            // TODO: Implement actual log monitoring logic
            // This is a placeholder - you would connect to your log service here
        }

        /// <summary>
        /// Check if monitoring can be started
        /// </summary>
        private bool CanStartMonitoring()
        {
            return !IsMonitoring;
        }

        /// <summary>
        /// Stop log monitoring
        /// </summary>
        private void StopMonitoring()
        {
            IsMonitoring = false;
            Status = "Stopped";

            // Add stop log entry
            LogContent += $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Log monitoring stopped\n";

            // TODO: Implement actual log monitoring cleanup
        }

        /// <summary>
        /// Check if monitoring can be stopped
        /// </summary>
        private bool CanStopMonitoring()
        {
            return IsMonitoring;
        }

        #endregion
    }
}
