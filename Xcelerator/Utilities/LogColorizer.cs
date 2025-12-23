using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Xcelerator.Models;

namespace Xcelerator.Utilities
{
    /// <summary>
    /// Utility class for colorizing log entries with syntax highlighting
    /// </summary>
    public static class LogColorizer
    {
        // Color definitions for light background theme
        private static readonly SolidColorBrush BackgroundBrush = new SolidColorBrush(Colors.White); // White background
        private static readonly SolidColorBrush DefaultTextBrush = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x2D)); // #2D2D2D - Dark Charcoal
        private static readonly SolidColorBrush SelectionBrush = new SolidColorBrush(Color.FromRgb(0xE3, 0xF2, 0xFD)); // #E3F2FD - Light Blue
        private static readonly SolidColorBrush AlternatingRowBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)); // #F5F5F5 - Light Gray
        private static readonly SolidColorBrush TimestampBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)); // #2E7D32 - Forest Green
        private static readonly SolidColorBrush FatalBrush = new SolidColorBrush(Color.FromRgb(0xB0, 0x00, 0x20)); // #B00020 - Dark Crimson
        private static readonly SolidColorBrush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xD3, 0x2F, 0x2F)); // #D32F2F - Brick Red
        private static readonly SolidColorBrush WarnBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x6C, 0x00)); // #EF6C00 - Burnt Orange
        private static readonly SolidColorBrush InfoBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x60, 0xA0)); // #0060A0 - Strong Blue
        private static readonly SolidColorBrush DebugBrush = new SolidColorBrush(Color.FromRgb(0x75, 0x75, 0x75)); // #757575 - Medium Gray
        private static readonly SolidColorBrush TraceBrush = new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E)); // #9E9E9E - Light Gray (for TRACE)
        private static readonly SolidColorBrush ThreadIdBrush = new SolidColorBrush(Color.FromRgb(0x6A, 0x1B, 0x9A)); // #6A1B9A - Deep Purple
        private static readonly SolidColorBrush MethodBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x79, 0x6B)); // #00796B - Teal
        private static readonly SolidColorBrush ExceptionBrush = new SolidColorBrush(Color.FromRgb(0xD3, 0x2F, 0x2F)); // #D32F2F - Brick Red (same as ERROR)

        static LogColorizer()
        {
            // Freeze brushes for performance
            BackgroundBrush.Freeze();
            DefaultTextBrush.Freeze();
            SelectionBrush.Freeze();
            AlternatingRowBrush.Freeze();
            TimestampBrush.Freeze();
            FatalBrush.Freeze();
            ErrorBrush.Freeze();
            WarnBrush.Freeze();
            InfoBrush.Freeze();
            DebugBrush.Freeze();
            TraceBrush.Freeze();
            ThreadIdBrush.Freeze();
            MethodBrush.Freeze();
            ExceptionBrush.Freeze();
        }

        /// <summary>
        /// Regex pattern to match timestamp: MM/dd/yyyy HH:mm:ss.fff
        /// </summary>
        private static readonly Regex TimestampRegex = new Regex(
            @"^(\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2}:\d{2}\.\d{3})",
            RegexOptions.Compiled);

        /// <summary>
        /// Regex pattern to match log level keywords
        /// </summary>
        private static readonly Regex LogLevelRegex = new Regex(
            @"\b(FATAL|ERROR|WARN|WARNING|INFO|DEBUG|TRACE)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Regex pattern to match thread IDs like [Thread-123] or [12]
        /// </summary>
        private static readonly Regex ThreadIdRegex = new Regex(
            @"\[(Thread-\d+|\d+)\]",
            RegexOptions.Compiled);

        /// <summary>
        /// Regex pattern to match method names like ClassName.MethodName
        /// </summary>
        private static readonly Regex MethodRegex = new Regex(
            @"\b([A-Z][a-zA-Z0-9]*\.)+[a-zA-Z0-9]+\b",
            RegexOptions.Compiled);

        /// <summary>
        /// Regex pattern to match exception keywords
        /// </summary>
        private static readonly Regex ExceptionRegex = new Regex(
            @"\b(Exception|Error|Throwable|at\s+[\w\.]+):",
            RegexOptions.Compiled);

        /// <summary>
        /// Parses a log line and returns colored text segments
        /// </summary>
        public static List<ColoredTextSegment> ColorizeLogLine(string logLine)
        {
            var segments = new List<ColoredTextSegment>();
            
            if (string.IsNullOrEmpty(logLine))
            {
                return segments;
            }

            int currentIndex = 0;

            // 1. Check for timestamp at the beginning
            var timestampMatch = TimestampRegex.Match(logLine);
            if (timestampMatch.Success && timestampMatch.Index == 0)
            {
                segments.Add(new ColoredTextSegment
                {
                    Text = timestampMatch.Value,
                    Brush = TimestampBrush
                });
                currentIndex = timestampMatch.Length;

                // Add space after timestamp if exists
                if (currentIndex < logLine.Length && logLine[currentIndex] == ' ')
                {
                    segments.Add(new ColoredTextSegment
                    {
                        Text = " ",
                        Brush = DefaultTextBrush
                    });
                    currentIndex++;
                }
            }

            // 2. Check for log level
            var remainingText = logLine.Substring(currentIndex);
            var logLevelMatch = LogLevelRegex.Match(remainingText);
            
            if (logLevelMatch.Success && logLevelMatch.Index < 50) // Log level should be near the beginning
            {
                // Add text before log level
                if (logLevelMatch.Index > 0)
                {
                    segments.Add(new ColoredTextSegment
                    {
                        Text = remainingText.Substring(0, logLevelMatch.Index),
                        Brush = DefaultTextBrush
                    });
                }

                // Add log level with appropriate color
                var logLevelText = logLevelMatch.Value.ToUpper();
                var levelBrush = logLevelText switch
                {
                    "FATAL" => FatalBrush,
                    "ERROR" => ErrorBrush,
                    "WARN" or "WARNING" => WarnBrush,
                    "INFO" => InfoBrush,
                    "DEBUG" => DebugBrush,
                    "TRACE" => TraceBrush,
                    _ => DefaultTextBrush
                };

                segments.Add(new ColoredTextSegment
                {
                    Text = logLevelMatch.Value,
                    Brush = levelBrush
                });

                currentIndex += logLevelMatch.Index + logLevelMatch.Length;
            }

            // 3. Process the rest of the line for special patterns
            remainingText = logLine.Substring(currentIndex);
            ProcessRemainingText(remainingText, segments);

            return segments;
        }

        /// <summary>
        /// Processes the remaining text for thread IDs, methods, exceptions, etc.
        /// </summary>
        private static void ProcessRemainingText(string text, List<ColoredTextSegment> segments)
        {
            if (string.IsNullOrEmpty(text))
                return;

            // Check if line contains exception keywords
            bool hasException = ExceptionRegex.IsMatch(text);

            int lastIndex = 0;

            // Find all thread IDs
            var threadMatches = ThreadIdRegex.Matches(text);
            
            foreach (Match match in threadMatches)
            {
                // Add text before thread ID
                if (match.Index > lastIndex)
                {
                    var beforeText = text.Substring(lastIndex, match.Index - lastIndex);
                    AddTextWithMethodHighlighting(beforeText, segments, hasException);
                }

                // Add thread ID
                segments.Add(new ColoredTextSegment
                {
                    Text = match.Value,
                    Brush = ThreadIdBrush
                });

                lastIndex = match.Index + match.Length;
            }

            // Add remaining text
            if (lastIndex < text.Length)
            {
                var remainingText = text.Substring(lastIndex);
                AddTextWithMethodHighlighting(remainingText, segments, hasException);
            }
        }

        /// <summary>
        /// Adds text segments with method name highlighting
        /// </summary>
        private static void AddTextWithMethodHighlighting(string text, List<ColoredTextSegment> segments, bool hasException)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var methodMatches = MethodRegex.Matches(text);
            
            if (methodMatches.Count == 0)
            {
                // No methods found, add as default text
                segments.Add(new ColoredTextSegment
                {
                    Text = text,
                    Brush = hasException ? ExceptionBrush : DefaultTextBrush
                });
                return;
            }

            int lastIndex = 0;

            foreach (Match match in methodMatches)
            {
                // Add text before method
                if (match.Index > lastIndex)
                {
                    segments.Add(new ColoredTextSegment
                    {
                        Text = text.Substring(lastIndex, match.Index - lastIndex),
                        Brush = hasException ? ExceptionBrush : DefaultTextBrush
                    });
                }

                // Add method name
                segments.Add(new ColoredTextSegment
                {
                    Text = match.Value,
                    Brush = MethodBrush
                });

                lastIndex = match.Index + match.Length;
            }

            // Add remaining text
            if (lastIndex < text.Length)
            {
                segments.Add(new ColoredTextSegment
                {
                    Text = text.Substring(lastIndex),
                    Brush = hasException ? ExceptionBrush : DefaultTextBrush
                });
            }
        }

        /// <summary>
        /// Gets the background brush
        /// </summary>
        public static SolidColorBrush GetBackgroundBrush() => BackgroundBrush;

        /// <summary>
        /// Gets the selection brush
        /// </summary>
        public static SolidColorBrush GetSelectionBrush() => SelectionBrush;
    }
}
