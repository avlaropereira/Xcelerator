using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Xcelerator.Models
{
    /// <summary>
    /// Represents a highlight setting with color and state information.
    /// Colors are converted from signed integers during deserialization for performance.
    /// </summary>
    public class HighlightSetting : INotifyPropertyChanged
    {
        private bool _isSelected;

        public Color BackColor { get; set; }
        public Color BorderColor { get; set; }
        public Color MarkerColor { get; set; }
        public int Flags { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Creates frozen brushes for performance optimization.
        /// Frozen brushes are read-only and thread-safe, reducing composition engine overhead.
        /// </summary>
        public SolidColorBrush BackgroundBrush
        {
            get
            {
                var brush = new SolidColorBrush(BackColor);
                brush.Freeze();
                return brush;
            }
        }

        public SolidColorBrush BorderBrush
        {
            get
            {
                var brush = new SolidColorBrush(BorderColor);
                brush.Freeze();
                return brush;
            }
        }

        public SolidColorBrush MarkerBrush
        {
            get
            {
                var brush = new SolidColorBrush(MarkerColor);
                brush.Freeze();
                return brush;
            }
        }

        /// <summary>
        /// Converts a signed integer (ARGB format) to a Color.
        /// </summary>
        public static Color IntToColor(int argb)
        {
            byte a = (byte)((argb >> 24) & 0xFF);
            byte r = (byte)((argb >> 16) & 0xFF);
            byte g = (byte)((argb >> 8) & 0xFF);
            byte b = (byte)(argb & 0xFF);
            return Color.FromArgb(a, r, g, b);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
