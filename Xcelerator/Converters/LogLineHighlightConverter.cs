using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Xcelerator.Models;
using Xcelerator.Utilities;

namespace Xcelerator.Converters
{
    /// <summary>
    /// Multi-value converter that highlights search matches in log lines with the selected highlight color.
    /// Takes: [0] log line (string), [1] search text (string), [2] selected highlight (HighlightSetting)
    /// Returns: List of TextSegment for rendering
    /// </summary>
    public class LogLineHighlightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3)
                return GetColoredSegments(values[0]?.ToString() ?? string.Empty);

            var logLine = values[0]?.ToString() ?? string.Empty;
            var searchText = values[1]?.ToString() ?? string.Empty;
            var selectedHighlight = values[2] as HighlightSetting;

            // If no search text or no highlight selected, use default colorization
            if (string.IsNullOrWhiteSpace(searchText) || selectedHighlight == null)
            {
                return GetColoredSegments(logLine);
            }

            // Get the base colored segments from LogColorizer
            var baseSegments = GetColoredSegments(logLine);

            // Apply highlight to matching segments
            var highlightedSegments = new List<TextSegment>();
            
            foreach (var segment in baseSegments)
            {
                if (segment.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                {
                    // This segment contains the search text, apply highlighting
                    highlightedSegments.AddRange(HighlightMatches(segment.Text, searchText, segment.Brush, selectedHighlight));
                }
                else
                {
                    // No match, keep original segment
                    highlightedSegments.Add(segment);
                }
            }

            return highlightedSegments;
        }

        /// <summary>
        /// Gets colored segments using the existing LogColorizer
        /// </summary>
        private List<TextSegment> GetColoredSegments(string logLine)
        {
            var coloredSegments = LogColorizer.ColorizeLogLine(logLine);

            // Convert ColoredTextSegment to TextSegment
            var segments = new List<TextSegment>();
            foreach (var seg in coloredSegments)
            {
                segments.Add(new TextSegment
                {
                    Text = seg.Text,
                    Brush = seg.Brush,
                    BackgroundBrush = null
                });
            }
            return segments;
        }

        /// <summary>
        /// Highlights all occurrences of searchText in the text segment
        /// </summary>
        private List<TextSegment> HighlightMatches(string text, string searchText, Brush defaultBrush, HighlightSetting highlight)
        {
            var segments = new List<TextSegment>();
            var searchComparison = StringComparison.OrdinalIgnoreCase;
            int startIndex = 0;

            while (startIndex < text.Length)
            {
                int matchIndex = text.IndexOf(searchText, startIndex, searchComparison);
                
                if (matchIndex == -1)
                {
                    // No more matches, add remaining text
                    if (startIndex < text.Length)
                    {
                        segments.Add(new TextSegment
                        {
                            Text = text.Substring(startIndex),
                            Brush = defaultBrush
                        });
                    }
                    break;
                }

                // Add text before match
                if (matchIndex > startIndex)
                {
                    segments.Add(new TextSegment
                    {
                        Text = text.Substring(startIndex, matchIndex - startIndex),
                        Brush = defaultBrush
                    });
                }

                // Add highlighted match with background
                var matchedText = text.Substring(matchIndex, searchText.Length);
                segments.Add(new TextSegment
                {
                    Text = matchedText,
                    Brush = new SolidColorBrush(Colors.Black), // Text color for readability
                    BackgroundBrush = highlight.BackgroundBrush
                });

                startIndex = matchIndex + searchText.Length;
            }

            return segments;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Extended TextSegment to support background highlighting
    /// </summary>
    public class TextSegment
    {
        public string Text { get; set; } = string.Empty;
        public Brush Brush { get; set; } = Brushes.Black;
        public Brush? BackgroundBrush { get; set; }
    }
}
