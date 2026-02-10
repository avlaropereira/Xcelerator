using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Xcelerator.Converters
{
    /// <summary>
    /// Converts a string to Visibility. Empty/null strings = Collapsed, non-empty = Visible.
    /// Use parameter="Inverted" to invert the logic.
    /// </summary>
    public class StringToVisibilityInvertedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;
            bool isEmpty = string.IsNullOrWhiteSpace(str);
            
            // Default behavior: empty = Collapsed, non-empty = Visible
            // With "Inverted" parameter: empty = Visible, non-empty = Collapsed
            bool inverted = parameter?.ToString()?.Equals("Inverted", StringComparison.OrdinalIgnoreCase) == true;
            
            if (inverted)
            {
                return isEmpty ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return isEmpty ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
