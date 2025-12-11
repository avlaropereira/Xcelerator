using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Xcelerator.Converters
{
    /// <summary>
    /// Converts null to Visibility with inverted logic (null = Visible, not null = Collapsed)
    /// </summary>
    public class NullToVisibilityInvertedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
