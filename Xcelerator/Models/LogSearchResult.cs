namespace Xcelerator.Models
{
    /// <summary>
    /// Represents a single log search result
    /// </summary>
    public class LogSearchResult
    {
        /// <summary>
        /// The tab name where the match was found
        /// </summary>
        public string TabName { get; set; } = string.Empty;

        /// <summary>
        /// The complete log entry that contains the match
        /// </summary>
        public string LogEntry { get; set; } = string.Empty;

        /// <summary>
        /// The line number (1-based) in the original log file for display purposes
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Preview of the match context (shortened for display)
        /// </summary>
        public string Preview { get; set; } = string.Empty;

        /// <summary>
        /// Reference to the source tab view model
        /// </summary>
        public object? SourceTab { get; set; }

        /// <summary>
        /// Display text for the list (combines tab name and preview)
        /// </summary>
        public string DisplayText => $"[{TabName}] Line {LineNumber}: {Preview}";

        /// <summary>
        /// Dummy property for TreeView binding compatibility (leaf nodes don't expand)
        /// This prevents binding errors when TreeViewItem tries to bind IsExpanded to all items
        /// </summary>
        public bool IsExpanded { get; set; } = false;
    }
}
