using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Xcelerator.Converters
{
    /// <summary>
    /// Converter that determines if a refresh interval item should be visible based on minimum threshold
    /// </summary>
    public class RefreshIntervalItemVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
                return Visibility.Visible;

            if (values[0] is int itemValue && values[1] is double loadTimeSeconds)
            {
                // Calculate minimum refresh interval (load time + 1 minute)
                int loadTimeMinutes = (int)Math.Ceiling(loadTimeSeconds / 60.0);
                int minimumRefreshMinutes = loadTimeMinutes + 1;

                // Show item if it's "Off" (0) or meets the minimum threshold
                return itemValue == 0 || itemValue >= minimumRefreshMinutes 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
