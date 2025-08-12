using System.Windows.Input;
using Xcelerator.Models;

namespace Xcelerator.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private object _currentPage;
        private LoginCredentials _credentials;

        public MainViewModel()
        {
            _credentials = new LoginCredentials();
            _currentPage = new PanelViewModel(this);
            
            NavigateToLoginCommand = new RelayCommand(NavigateToLogin, CanNavigateToLogin);
            NavigateToDashboardCommand = new RelayCommand(NavigateToDashboard);
            NavigateBackCommand = new RelayCommand(NavigateBack);
        }

        public object CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public LoginCredentials Credentials
        {
            get => _credentials;
            set => SetProperty(ref _credentials, value);
        }

        public ICommand NavigateToLoginCommand { get; }
        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateBackCommand { get; }

        private void NavigateToLogin()
        {
            // This method is no longer needed as login is handled by PanelViewModel
            // The PanelViewModel will handle login navigation internally
        }

        private bool CanNavigateToLogin()
        {
            return Credentials.SelectedClusters.Any();
        }

        private void NavigateToDashboard()
        {
            // This method is no longer needed as dashboard navigation is handled by PanelViewModel
            // The PanelViewModel will handle dashboard navigation internally
        }

        private void NavigateBack()
        {
            // This method is no longer needed as navigation is handled by PanelViewModel
            // The PanelViewModel will handle all internal navigation
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
        public void Execute(object? parameter) => _execute((T?)parameter);
    }
}
