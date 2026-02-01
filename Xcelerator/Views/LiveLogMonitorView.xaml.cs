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

            // Get the TreeViewItem that was double-clicked
            if (sender is not TreeViewItem treeViewItem)
                return;

            // Only handle double-click for leaf items (LogSearchResult), not group headers
            if (treeViewItem.DataContext is not LogSearchResult searchResult)
                return;

            // Get the ViewModel
            if (DataContext is not LiveLogMonitorViewModel viewModel)
                return;

            // Execute the navigation command
            if (viewModel.NavigateToSearchResultCommand.CanExecute(searchResult))
            {
                viewModel.NavigateToSearchResultCommand.Execute(searchResult);
            }

            // Mark the event as handled
            e.Handled = true;
        }

        /// <summary>
        /// Handles the collapse button click to collapse a specific group
        /// </summary>
        private void CollapseGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is LogSearchResultGroup group)
            {
                group.IsExpanded = false;
            }
        }

        /// <summary>
        /// Handles the chevron icon click to toggle group expansion
        /// </summary>
        private void ToggleGroupExpansion_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is LogSearchResultGroup group)
            {
                group.IsExpanded = !group.IsExpanded;
            }
        }

        /// <summary>
        /// Handles double-click on group header to toggle expansion
        /// </summary>
        private void GroupHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Check for double-click
            if (e.ClickCount == 2 && sender is TextBlock textBlock && textBlock.DataContext is LogSearchResultGroup group)
            {
                group.IsExpanded = !group.IsExpanded;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the add server button click to show the modal dialog
        /// </summary>
        private void AddServerButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the ViewModel
            if (DataContext is not LiveLogMonitorViewModel viewModel)
                return;

            var dialog = new AddServerDialog(viewModel.ClusterName)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true && dialog.ServerAdded)
            {
                // Server was successfully added to the JSON file
                // Automatically reload the topology to show the new server
                viewModel.ReloadTopology();
            }
        }
    }
}


