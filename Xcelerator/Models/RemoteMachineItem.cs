using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Xcelerator.Models
{
    /// <summary>
    /// Represents a remote machine item with optional child items for hierarchical display
    /// </summary>
    public class RemoteMachineItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _displayName = string.Empty;
        private bool _isExpanded = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public ObservableCollection<RemoteMachineItem> Children { get; set; } = new ObservableCollection<RemoteMachineItem>();
        
        /// <summary>
        /// Indicates if this item has child items
        /// </summary>
        public bool HasChildren => Children.Count > 0;
        
        /// <summary>
        /// Indicates if this is a child/leaf item (no children)
        /// </summary>
        public bool IsLeaf => Children.Count == 0;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
