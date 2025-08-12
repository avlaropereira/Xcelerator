using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Xcelerator.ViewModels;

namespace Xcelerator.Converters
{
    public class LoginFormVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] != null && values[1] is bool isInDashboardMode)
            {
                // Show login form if cluster is selected AND we're not in dashboard mode
                return !isInDashboardMode ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
