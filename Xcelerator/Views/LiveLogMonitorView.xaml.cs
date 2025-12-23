using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xcelerator.Models;
using Xcelerator.ViewModels;

namespace Xcelerator.Views
{
    /// <summary>
    /// Interaction logic for LiveLogMonitorView.xaml
    /// </summary>
    public partial class LiveLogMonitorView : UserControl
    {
        public LiveLogMonitorView()
        {
            InitializeComponent();
        }

        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            // Get the TreeViewItem that was double-clicked
            if (sender is not TreeViewItem treeViewItem)
                return;

            // Get the RemoteMachineItem from the DataContext
            if (treeViewItem.DataContext is not RemoteMachineItem remoteMachine)
                return;

            // Get the ViewModel
            if (DataContext is not LiveLogMonitorViewModel viewModel)
                return;

            // Execute the command if it can execute
            if (viewModel.OpenMachineTabCommand.CanExecute(remoteMachine))
            {
                viewModel.OpenMachineTabCommand.Execute(remoteMachine);
            }

            // Mark the event as handled to prevent it from bubbling up
            e.Handled = true;
        }

        private void SearchResult_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            // Get the ViewModel
            if (DataContext is not LiveLogMonitorViewModel viewModel)
                return;

            // Get the selected search result
            var searchResult = viewModel.SelectedSearchResult;
            if (searchResult == null)
                return;

            // Execute the navigation command
            if (viewModel.NavigateToSearchResultCommand.CanExecute(searchResult))
            {
                viewModel.NavigateToSearchResultCommand.Execute(searchResult);
            }

            // Mark the event as handled
            e.Handled = true;
        }
    }
}

