using System.Windows.Input;
using Xcelerator.Models;

namespace Xcelerator.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private string _accessKey = string.Empty;
        private string _secretKey = string.Empty;

        public LoginViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            
            SignInCommand = new RelayCommand(SignIn, CanSignIn);
            GoBackCommand = new RelayCommand(GoBack);
        }

        public string AccessKey
        {
            get => _accessKey;
            set
            {
                SetProperty(ref _accessKey, value);
                _mainViewModel.Credentials.AccessKey = value;
            }
        }

        public string SecretKey
        {
            get => _secretKey;
            set
            {
                SetProperty(ref _secretKey, value);
                _mainViewModel.Credentials.SecretKey = value;
            }
        }

        public ICommand SignInCommand { get; }
        public ICommand GoBackCommand { get; }

        private void SignIn()
        {
            // For demo purposes, accept any non-empty credentials
            if (CanSignIn())
            {
                _mainViewModel.NavigateToDashboardCommand.Execute(null);
            }
        }

        private bool CanSignIn()
        {
            return !string.IsNullOrWhiteSpace(AccessKey) && !string.IsNullOrWhiteSpace(SecretKey);
        }

        private void GoBack()
        {
            _mainViewModel.NavigateBackCommand.Execute(null);
        }
    }
}
