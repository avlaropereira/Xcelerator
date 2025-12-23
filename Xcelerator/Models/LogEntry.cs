using System.Windows.Media;

namespace Xcelerator.Models
{
    /// <summary>
    /// Represents a text segment with a specific color for syntax highlighting
    /// </summary>
    public class ColoredTextSegment
    {
        public string Text { get; set; } = string.Empty;
        public SolidColorBrush Brush { get; set; } = Brushes.White;
    }

    /// <summary>
    /// Represents log level types
    /// </summary>
    public enum LogLevel
    {
        None,
        FATAL,
        ERROR,
        WARN,
        INFO,
        DEBUG,
        TRACE
    }

    /// <summary>
    /// Parsed log entry with identified components
    /// </summary>
    public class LogEntry
    {
        public string Timestamp { get; set; } = string.Empty;
        public LogLevel Level { get; set; } = LogLevel.None;
        public string Message { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
    }
}
