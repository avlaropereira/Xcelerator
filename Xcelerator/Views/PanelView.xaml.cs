using System.Windows;
using System.Windows.Controls;
using Xcelerator.ViewModels;

namespace Xcelerator.Views
{
    /// <summary>
    /// Interaction logic for PanelView.xaml
    /// </summary>
    public partial class PanelView : UserControl
    {
        public PanelView()
        {
            InitializeComponent();
            AvailableClustersList.MouseDoubleClick += AvailableClustersList_MouseDoubleClick;
        }

        private void AvailableClustersList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AvailableClustersList.SelectedItem is Models.Cluster cluster && DataContext is PanelViewModel viewModel)
            {
                viewModel.SelectClusterCommand.Execute(cluster);
            }
        }
    }
}
