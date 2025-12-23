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
                    ScrollToSelectedItem();
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

