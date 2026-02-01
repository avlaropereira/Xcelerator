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
    }
}

