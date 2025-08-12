using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Xcelerator.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                bool shouldShow = boolValue;
                
                // If parameter is "Invert", invert the boolean
                if (parameter is string param && param == "Invert")
                {
                    shouldShow = !shouldShow;
                }
                
                return shouldShow ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
