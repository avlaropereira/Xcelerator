# Search and Highlight Feature

## Overview
The Search and Highlight feature allows users to search for specific text in log files and highlight all matching occurrences with a selected color. This feature seamlessly integrates with the Highlight Panel and works efficiently with the virtualized log viewer.

## How It Works

### User Workflow
1. **Open Highlight Panel**: Click the palette icon (🎨) in the status bar
2. **Enter Search Text**: Type text into the search box in the highlight panel
3. **Select Color**: Click a color swatch to apply that highlight color
4. **View Results**: All matching log lines are highlighted with the selected background color
5. **Match Count**: See the total number of matching lines below the search box

### Architecture

#### ViewModel Integration (LogTabViewModel.cs)
- **SearchText** property: Bound to the search textbox, triggers match counting
- **MatchCount** property: Displays the number of lines containing the search text
- **SelectedHighlight** property: The chosen highlight color
- **UpdateMatchCount()**: Efficiently counts matches using case-insensitive search
- **ClearSearch()**: Resets search text and highlight selection

#### Converter (LogLineHighlightConverter.cs)
A high-performance multi-value converter that:
1. Takes three inputs: log line text, search text, selected highlight
2. Uses existing `LogColorizer.ColorizeLogLine()` for syntax highlighting
3. Applies background highlighting to matching text segments
4. Preserves original syntax colors while adding highlight background
5. Returns `TextSegment` objects with both foreground and background brushes

#### UI Components (LogMonitorView.xaml)

**Search Box:**
- Located at the top of the Highlight Panel
- Real-time search with `UpdateSourceTrigger=PropertyChanged`
- Modern design with search icon and clear button
- Clear button (❌) appears only when text is entered
- Match count displays below the search box

**Log Line Display:**
- Uses `MultiBinding` to combine log line, search text, and selected highlight
- `LogLineHighlightConverter` processes each line for rendering
- Wraps highlighted text in `Border` elements with background color
- Maintains virtualization for performance

### Performance Optimizations

1. **Efficient Match Counting**
   - Uses `StringComparison.OrdinalIgnoreCase` for fast searching
   - Counts on-demand, triggered by search text changes
   - Doesn't block UI thread

2. **Virtualization-Friendly**
   - Highlighting applied during item rendering
   - No modification of source data
   - Works seamlessly with `VirtualizingPanel.IsVirtualizing="True"`

3. **Frozen Brushes**
   - Highlight colors are frozen `SolidColorBrush` instances
   - Reduces WPF composition engine overhead
   - Brushes are reused across all matching segments

4. **Incremental Processing**
   - Only visible items are processed due to virtualization
   - Scrolling performance remains smooth even with large logs

### Visual Design

**Search Box Styling:**
- White background with light border (#FFE0E0E0)
- Rounded corners (4px border radius)
- Search icon (🔍) using Segoe MDL2 Assets font
- Clear button with hover state
- Match count in subdued gray text

**Highlighted Text:**
- Selected highlight color as background
- Black text for readability on colored backgrounds
- 2px border radius for soft appearance
- Seamlessly blends with existing syntax highlighting

### Code Examples

#### MultiBinding in XAML
```xaml
<ItemsControl.ItemsSource>
    <MultiBinding Converter="{StaticResource LogLineHighlightConverter}">
        <Binding/>  <!-- Log line text -->
        <Binding Path="DataContext.SearchText" RelativeSource="{RelativeSource AncestorType=UserControl}"/>
        <Binding Path="DataContext.SelectedHighlight" RelativeSource="{RelativeSource AncestorType=UserControl}"/>
    </MultiBinding>
</ItemsControl.ItemsSource>
```

#### Converter Logic
```csharp
// Apply highlight to matching segments
foreach (var segment in baseSegments)
{
    if (segment.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
    {
        highlightedSegments.AddRange(
            HighlightMatches(segment.Text, searchText, segment.Brush, selectedHighlight)
        );
    }
    else
    {
        highlightedSegments.Add(segment);
    }
}
```

## Usage Examples

### Basic Search
1. Open highlight panel
2. Type "ERROR" in search box
3. See "47 matches found"
4. Select red color swatch
5. All "ERROR" text is highlighted in red

### Case-Insensitive Search
- Searching for "exception" matches "Exception", "EXCEPTION", "exception"
- Useful for finding log patterns regardless of casing

### Clear Search
- Click the ❌ button to clear search text
- Or manually select all text and delete
- Match count resets to 0

## Technical Details

### TextSegment Model
```csharp
public class TextSegment
{
    public string Text { get; set; }
    public Brush Brush { get; set; }              // Foreground color (syntax highlighting)
    public Brush? BackgroundBrush { get; set; }   // Background color (search highlighting)
}
```

### Match Counting Algorithm
- O(n) complexity where n = number of log lines
- Case-insensitive comparison
- Simple substring match (no regex overhead)
- Updates in real-time as user types

### Integration with Existing Features
- Works alongside syntax highlighting (timestamps, log levels, etc.)
- Compatible with detail panel selection
- Doesn't interfere with auto-refresh functionality
- Persists during log refresh operations
- **Tab Independent**: Each tab maintains its own search text and highlight selection

### Tab Independence

The search and highlight feature is fully independent per tab:

**Per-Tab Search State:**
- Each tab has its own `SearchText`, `MatchCount`, and `SelectedHighlight`
- Switching tabs preserves search state for each individual tab
- You can search for "ERROR" in one tab and "WARNING" in another simultaneously

**Automatic State Restoration:**
When you switch between tabs, the view automatically:
1. Restores the search text for that tab
2. Updates the match count
3. Reapplies the selected highlight color
4. Shows/hides the highlight panel based on that tab's state

**Implementation Details:**
The `LogMonitorView` handles tab switching through event handlers that restore both the highlight panel visibility and the search state. Since `SearchText` and `SelectedHighlight` are bound to the ViewModel, they automatically update when the `DataContext` changes.

**User Experience:**
- Open Tab A, search for "ERROR", select red highlight
- Switch to Tab B, search for "INFO", select blue highlight  
- Switch back to Tab A - "ERROR" search with red highlight is still active
- Switch to Tab B - "INFO" search with blue highlight is still active

## Future Enhancements

1. **Regular Expression Support**
   - Toggle between simple text and regex search
   - Pattern matching for complex queries

2. **Search History**
   - Dropdown of recent searches
   - Quick re-apply of previous searches

3. **Multiple Highlight Colors**
   - Allow multiple search terms with different colors
   - Visual differentiation of different search criteria

4. **Context Lines**
   - Show lines before/after matches
   - Configurable context line count

5. **Export Highlighted Results**
   - Copy only highlighted lines
   - Export to file with formatting preserved

6. **Keyboard Shortcuts**
   - Ctrl+F to focus search box
   - F3/Shift+F3 to navigate between matches
   - Escape to clear search

## Known Limitations

1. **Partial Line Matching**
   - Matches anywhere in the line, not just specific fields
   - Cannot restrict search to timestamps, log levels, etc.

2. **No Find/Replace**
   - Read-only highlighting, no text modification

3. **Single Active Search**
   - Only one search term active at a time
   - Cannot combine multiple search criteria

## Best Practices

1. **Performance**: Avoid searching for very common terms (e.g., "a", "the") which may highlight thousands of lines
2. **Visibility**: Choose highlight colors with good contrast for readability
3. **Workflow**: Use search to identify patterns, then select individual lines for detailed analysis in the detail panel
