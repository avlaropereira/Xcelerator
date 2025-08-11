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
        }

        private void AvailableClustersList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AvailableClustersList.SelectedItem is Models.Cluster cluster && DataContext is ClusterSelectionViewModel viewModel)
            {
                viewModel.SelectClusterCommand.Execute(cluster);
            }
        }
    }
}
