using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Xcelerator.Models
{
    /// <summary>
    /// Represents a group of search results from a single tab, supporting hierarchical display
    /// </summary>
    public class LogSearchResultGroup : INotifyPropertyChanged
    {
        private string _tabName = string.Empty;
        private bool _isExpanded = true;
        private ObservableCollection<LogSearchResult> _results;

        public event PropertyChangedEventHandler? PropertyChanged;

        public LogSearchResultGroup()
        {
            _results = new ObservableCollection<LogSearchResult>();
        }

        /// <summary>
        /// The tab name that groups these results
        /// </summary>
        public string TabName
        {
            get => _tabName;
            set => SetProperty(ref _tabName, value);
        }

        /// <summary>
        /// Whether the group is expanded in the TreeView
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        /// <summary>
        /// Collection of search results in this group
        /// </summary>
        public ObservableCollection<LogSearchResult> Results
        {
            get => _results;
            set => SetProperty(ref _results, value);
        }

        /// <summary>
        /// Number of results in this group
        /// </summary>
        public int ResultCount => Results.Count;

        /// <summary>
        /// Display text showing tab name and result count
        /// </summary>
        public string DisplayText => $"{TabName} ({ResultCount} matches)";

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            
            // When Results collection changes, notify DisplayText and ResultCount
            if (propertyName == nameof(Results))
            {
                OnPropertyChanged(nameof(ResultCount));
                OnPropertyChanged(nameof(DisplayText));
            }
            
            return true;
        }
    }
}
