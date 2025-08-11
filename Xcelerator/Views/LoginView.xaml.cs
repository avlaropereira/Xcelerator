using System.Windows;
using System.Windows.Controls;
using Xcelerator.ViewModels;

namespace Xcelerator.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            SecretKeyPasswordBox.PasswordChanged += SecretKeyPasswordBox_PasswordChanged;
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
