using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
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
            DataContextChanged += LogMonitorView_DataContextChanged;
            Loaded += LogMonitorView_Loaded;
            IsVisibleChanged += LogMonitorView_IsVisibleChanged;
        }

        private void LogMonitorView_Loaded(object sender, RoutedEventArgs e)
        {
            // Ensure panel states are correct when view is loaded
            if (DataContext is LogTabViewModel viewModel)
            {
                UpdateHighlightPanelColumnWidth(viewModel.IsHighlightPanelVisible);
                UpdateDetailPanelRowHeight(viewModel.IsDetailPanelVisible);
            }
        }

        private void LogMonitorView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // When tab becomes visible, restore its panel states
            if (e.NewValue is bool isVisible && isVisible && DataContext is LogTabViewModel viewModel)
            {
                UpdateHighlightPanelColumnWidth(viewModel.IsHighlightPanelVisible);
                UpdateDetailPanelRowHeight(viewModel.IsDetailPanelVisible);
            }
        }

        private void LogMonitorView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Unsubscribe from old DataContext
            if (e.OldValue is LogTabViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            // Subscribe to new DataContext
            if (e.NewValue is LogTabViewModel newViewModel)
            {
                newViewModel.PropertyChanged += ViewModel_PropertyChanged;

                // Restore the tab's highlight panel state when switching to this tab
                UpdateHighlightPanelColumnWidth(newViewModel.IsHighlightPanelVisible);

                // Restore detail panel state
                UpdateDetailPanelRowHeight(newViewModel.IsDetailPanelVisible);
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LogTabViewModel.SelectedLogLine) && sender is LogTabViewModel viewModel)
            {
                // Scroll to the selected item when SelectedLogLine changes
                if (!string.IsNullOrEmpty(viewModel.SelectedLogLine))
                {
                    // If detail panel is becoming visible, delay scroll until after layout update
                    if (viewModel.IsDetailPanelVisible)
                    {
                        // Use Dispatcher to scroll after the layout pass completes
                        Dispatcher.BeginInvoke(new Action(() => ScrollToSelectedItem()), 
                            System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                    else
                    {
                        ScrollToSelectedItem();
                    }
                }
            }
            else if (e.PropertyName == nameof(LogTabViewModel.IsDetailPanelVisible) && sender is LogTabViewModel viewModel2)
            {
                // Update detail panel row height based on visibility
                UpdateDetailPanelRowHeight(viewModel2.IsDetailPanelVisible);

                // If detail panel is opening and there's a selected line, ensure it's still visible
                if (viewModel2.IsDetailPanelVisible && !string.IsNullOrEmpty(viewModel2.SelectedLogLine))
                {
                    // Scroll after layout completes
                    Dispatcher.BeginInvoke(new Action(() => ScrollToSelectedItem()), 
                        System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
            else if (e.PropertyName == nameof(LogTabViewModel.IsHighlightPanelVisible) && sender is LogTabViewModel viewModel3)
            {
                // Update highlight panel column width based on visibility
                UpdateHighlightPanelColumnWidth(viewModel3.IsHighlightPanelVisible);
            }
        }

        private void ScrollToSelectedItem()
        {
            // Find the ListBox in the visual tree
            var listBox = FindName("LogLinesListBox") as ListBox;
            if (listBox != null && listBox.SelectedItem != null)
            {
                listBox.ScrollIntoView(listBox.SelectedItem);

                // Ensure the item container is brought into view as well
                listBox.UpdateLayout();
            }
        }

        /// <summary>
        /// Updates the detail panel row height to enable GridSplitter resizing
        /// </summary>
        private void UpdateDetailPanelRowHeight(bool isVisible)
        {
            var row = FindName("DetailPanelRow") as RowDefinition;
            if (row != null)
            {
                if (isVisible)
                {
                    // Set to 1* for star-based sizing, enabling GridSplitter to work
                    // MaxHeight is already bound to LogLinesListBox ActualHeight (50% constraint)
                    row.Height = new GridLength(1, GridUnitType.Star);
                }
                else
                {
                    // Collapse to 0 when hidden
                    row.Height = new GridLength(0, GridUnitType.Pixel);
                }
            }
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

        /// <summary>
        /// Handles the toggle button click to show/hide the highlight panel
        /// </summary>
        private void ToggleHighlightPanel_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LogTabViewModel viewModel)
            {
                viewModel.ToggleHighlightPanel();
                UpdateHighlightPanelColumnWidth(viewModel.IsHighlightPanelVisible);
            }
        }

        /// <summary>
        /// Handles the close button click to hide the highlight panel
        /// </summary>
        private void CloseHighlightPanel_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LogTabViewModel viewModel)
            {
                viewModel.IsHighlightPanelVisible = false;
                UpdateHighlightPanelColumnWidth(false);
            }
        }

        /// <summary>
        /// Handles the clear search button click
        /// </summary>
        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LogTabViewModel viewModel)
            {
                viewModel.ClearSearch();
            }
        }

        /// <summary>
        /// Handles the Paint/Select All button click
        /// </summary>
        private void PaintAll_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LogTabViewModel viewModel)
            {
                // TODO: Implement paint all matches functionality
                // This should highlight all matches with the selected color
                MessageBox.Show("Paint All functionality - Coming soon!", "Feature", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Handles the Unpaint/Clear All button click
        /// </summary>
        private void UnpaintAll_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LogTabViewModel viewModel)
            {
                // TODO: Implement unpaint all matches functionality
                // This should clear all highlights
                MessageBox.Show("Unpaint All functionality - Coming soon!", "Feature", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Handles the Go Up button click to navigate to previous match
        /// </summary>
        private void GoUp_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LogTabViewModel viewModel)
            {
                // TODO: Implement navigation to previous match
                // Find the previous log line that matches the search/highlight criteria
                MessageBox.Show("Go to Previous Match - Coming soon!", "Feature", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Handles the Go Down button click to navigate to next match
        /// </summary>
        private void GoDown_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LogTabViewModel viewModel)
            {
                // TODO: Implement navigation to next match
                // Find the next log line that matches the search/highlight criteria
                MessageBox.Show("Go to Next Match - Coming soon!", "Feature", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Handles the Collapse Logs button click to collapse matching lines
        /// </summary>
        private void CollapseLogs_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LogTabViewModel viewModel)
            {
                // TODO: Implement collapse matching lines functionality
                // This should hide all lines that match the current search/highlight
                MessageBox.Show("Collapse Matching Lines - Coming soon!", "Feature", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Handles the Undo Collapse button click to expand matching lines
        /// </summary>
        private void UndoCollapse_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LogTabViewModel viewModel)
            {
                // TODO: Implement undo collapse functionality
                // This should show all previously collapsed lines
                MessageBox.Show("Expand Matching Lines - Coming soon!", "Feature", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Updates the highlight panel column width based on visibility
        /// </summary>
        private void UpdateHighlightPanelColumnWidth(bool isVisible)
        {
            var column = FindName("HighlightPanelColumn") as ColumnDefinition;
            if (column != null)
            {
                column.Width = isVisible ? new GridLength(300) : new GridLength(0);
            }
        }
    }
}

