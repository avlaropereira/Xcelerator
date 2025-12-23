using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using Xcelerator.Models;
using Xcelerator.Utilities;

namespace Xcelerator.Converters
{
    /// <summary>
    /// Converts a log line string into a list of colored text segments
    /// </summary>
    public class LogLineToColoredSegmentsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string logLine)
            {
                return LogColorizer.ColorizeLogLine(logLine);
            }

            return new List<ColoredTextSegment>();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
