using System.Windows;
using System.Windows.Controls;
using Xcelerator.ViewModels;

namespace Xcelerator.Views
{
    /// <summary>
    /// Interaction logic for LogMonitorView.xaml
    /// </summary>
    public partial class LogMonitorView : UserControl
    {
        public LogMonitorView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the close button click to hide the detail panel
        /// </summary>
        private void CloseDetailPanel_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LogTabViewModel viewModel)
            {
                viewModel.SelectedLogLine = null;
                viewModel.IsDetailPanelVisible = false;
            }
        }
    }
}
