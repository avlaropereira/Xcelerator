using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Xcelerator.ViewModels;

namespace Xcelerator.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            SecretKeyPasswordBox.PasswordChanged += SecretKeyPasswordBox_PasswordChanged;
            DataContextChanged += LoginView_DataContextChanged;
        }

        private void LoginView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Unsubscribe from old view model
            if (e.OldValue is LoginViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            // Subscribe to new view model
            if (e.NewValue is LoginViewModel newViewModel)
            {
                newViewModel.PropertyChanged += ViewModel_PropertyChanged;
                
                // Initialize PasswordBox with current SecretKey value
                SecretKeyPasswordBox.Password = newViewModel.SecretKey;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoginViewModel.SecretKey) && sender is LoginViewModel viewModel)
            {
                // Only update PasswordBox if the value is different to avoid infinite loop
                if (SecretKeyPasswordBox.Password != viewModel.SecretKey)
                {
                    SecretKeyPasswordBox.Password = viewModel.SecretKey;
                }
            }
        }

        private void SecretKeyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.SecretKey = SecretKeyPasswordBox.Password;
            }
        }
    }
}
