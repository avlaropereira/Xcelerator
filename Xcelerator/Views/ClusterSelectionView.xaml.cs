using System.Windows;
using System.Windows.Controls;
using Xcelerator.ViewModels;

namespace Xcelerator.Views
{
    public partial class ClusterSelectionView : UserControl
    {
        public ClusterSelectionView()
        {
            InitializeComponent();
            AvailableClustersList.MouseDoubleClick += AvailableClustersList_MouseDoubleClick;
            SecretKeyPasswordBox.PasswordChanged += SecretKeyPasswordBox_PasswordChanged;
        }

        private void AvailableClustersList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AvailableClustersList.SelectedItem is Models.Cluster cluster && DataContext is ClusterSelectionViewModel viewModel)
            {
                viewModel.SelectClusterCommand.Execute(cluster);
            }
        }

        private void SecretKeyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ClusterSelectionViewModel viewModel)
            {
                viewModel.SecretKey = SecretKeyPasswordBox.Password;
            }
        }
    }
}
