# Search Result Navigation Implementation

## Overview
This document describes the implementation of search result navigation in the LiveLogMonitorView, allowing users to click on search results and navigate to the corresponding log entry in the appropriate tab.

## Implementation Details

### 1. User Experience Flow
1. User enters search text in the left panel
2. Search results appear grouped by tab in a TreeView
3. User double-clicks on a specific search result (leaf node)
4. The application:
   - Switches to the tab containing that log entry
   - Scrolls to the exact line in the log
   - Highlights the line
   - Opens the detail panel showing the full log entry

### 2. Key Components

#### LogTabViewModel - ScrollToLine Method
**File**: `Xcelerator/ViewModels/LogTabViewModel.cs`

```csharp
/// <summary>
/// Scrolls to a specific line number in the log
/// </summary>
/// <param name="lineNumber">The line number (0-based index) to scroll to</param>
public void ScrollToLine(int lineNumber)
{
    if (lineNumber < 0 || lineNumber >= LogLines.Count)
    {
        System.Diagnostics.Debug.WriteLine($"Line number {lineNumber} is out of range (0-{LogLines.Count - 1})");
        return;
    }

    // Set the selected log line to the target line
    SelectedLogLine = LogLines[lineNumber];
}
```

**Purpose**: 
- Provides a public method to programmatically scroll to a specific line number
- Validates the line number is within valid range
- Sets `SelectedLogLine` which triggers the UI to scroll and highlight the line

#### LiveLogMonitorViewModel - ExecuteNavigateToSearchResult
**File**: `Xcelerator/ViewModels/LiveLogMonitorViewModel.cs`

```csharp
/// <summary>
/// Navigates to the selected search result
/// </summary>
private void ExecuteNavigateToSearchResult(LogSearchResult? result)
{
    if (result == null || result.SourceTab is not LogTabViewModel tab)
        return;

    // Find the tab in the OpenTabs collection and make it the active tab
    var tabIndex = OpenTabs.IndexOf(tab);
    if (tabIndex >= 0)
    {
        // Switch to the tab containing the search result
        SelectedTabIndex = tabIndex;
        
        // Scroll to the specific line number in the tab
        // Note: LineNumber in LogSearchResult is 0-based
        tab.ScrollToLine(result.LineNumber);
    }
}
```

**Purpose**:
- Handles navigation when user double-clicks a search result
- Switches to the correct tab using `SelectedTabIndex`
- Calls `ScrollToLine` to scroll to the exact line

#### LiveLogMonitorView.xaml.cs - SearchResult_MouseDoubleClick
**File**: `Xcelerator/Views/LiveLogMonitorView.xaml.cs`

```csharp
private void SearchResult_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    if (e.ChangedButton != MouseButton.Left)
        return;

    // Get the TreeViewItem that was double-clicked
    if (sender is not TreeViewItem treeViewItem)
        return;

    // Only handle double-click for leaf items (LogSearchResult), not group headers
    if (treeViewItem.DataContext is not LogSearchResult searchResult)
        return;

    // Get the ViewModel
    if (DataContext is not LiveLogMonitorViewModel viewModel)
        return;

    // Execute the navigation command
    if (viewModel.NavigateToSearchResultCommand.CanExecute(searchResult))
    {
        viewModel.NavigateToSearchResultCommand.Execute(searchResult);
    }

    // Mark the event as handled
    e.Handled = true;
}
```

**Purpose**:
- Handles the UI event when user double-clicks a search result
- Filters out non-leaf items (group headers)
- Executes the `NavigateToSearchResultCommand`

#### LogMonitorView.xaml.cs - ViewModel_PropertyChanged
**File**: `Xcelerator/Views/LogMonitorView.xaml.cs`

```csharp
private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName == nameof(LogTabViewModel.SelectedLogLine) && sender is LogTabViewModel viewModel)
    {
        // Scroll to the selected item when SelectedLogLine changes
        if (!string.IsNullOrEmpty(viewModel.SelectedLogLine))
        {
            ScrollToSelectedItem();
        }
    }
    // ... other property handlers
}

private void ScrollToSelectedItem()
{
    // Find the ListBox in the visual tree
    var listBox = FindName("LogLinesListBox") as ListBox;
    if (listBox != null && listBox.SelectedItem != null)
    {
        listBox.ScrollIntoView(listBox.SelectedItem);
    }
}
```

**Purpose**:
- Listens for changes to `SelectedLogLine` property
- Automatically scrolls the ListBox to make the selected line visible
- Ensures smooth scrolling without breaking virtualization

### 3. Data Flow

```
User Double-Clicks Search Result
    ↓
SearchResult_MouseDoubleClick (View)
    ↓
NavigateToSearchResultCommand.Execute (ViewModel)
    ↓
ExecuteNavigateToSearchResult
    ↓
SelectedTabIndex = tabIndex (switches tab)
    ↓
tab.ScrollToLine(lineNumber)
    ↓
SelectedLogLine = LogLines[lineNumber]
    ↓
PropertyChanged event fired
    ↓
ViewModel_PropertyChanged (View)
    ↓
ScrollToSelectedItem
    ↓
ListBox.ScrollIntoView (UI scrolls and highlights)
```

### 4. Key Design Decisions

#### Why Not Directly Manipulate UI from ViewModel?
- Maintains MVVM pattern separation
- ViewModel sets `SelectedLogLine` property
- View listens to property changes and handles UI scrolling
- Allows for proper virtualization in ListBox

#### Why Use Line Number Instead of Line Content?
- More reliable for navigation (no duplicate content issues)
- Faster lookup (direct index access)
- Works with multi-line log entries

#### Why Two-Stage Selection (Tab + Line)?
- TabControl must switch tabs before scrolling
- Ensures the target ListBox is loaded and visible
- Prevents race conditions with virtualization

### 5. Performance Considerations

#### Virtualization Maintained
- ListBox uses `VirtualizingPanel.IsVirtualizing="True"`
- Only visible items are rendered
- Scrolling to line doesn't break virtualization
- Smooth performance even with 100K+ log lines

#### No Unnecessary Reloading
- Switching tabs doesn't reload log content
- Navigation is instant (no file I/O)
- Only UI scrolling and selection updates

#### Detail Panel Integration
- Setting `SelectedLogLine` automatically shows detail panel
- User can see full multi-line log entry
- Panel is responsive and follows grid splitter constraints

### 6. User Experience Features

#### Visual Feedback
- Selected line is highlighted with yellow background (`#FFFFFF99`)
- Border added to selected item (`BorderBrush="#FFCCCC00"`)
- Smooth scrolling animation
- Detail panel opens automatically

#### Mouse Interactions
- Hover shows light gray background (`#FFF5F5F5`)
- Double-click on leaf items navigates
- Double-click on group headers toggles expansion
- Single-click selects without navigating

#### Keyboard Support
- Can use arrow keys after navigation
- Tab key moves focus between panels
- Enter key in search box triggers search

## Testing Scenarios

1. **Single Match Navigation**
   - Search for unique text
   - Double-click result
   - Verify tab switches and line is visible

2. **Multi-Tab Navigation**
   - Open multiple tabs
   - Search for text in all tabs
   - Click results from different tabs
   - Verify correct tab selection each time

3. **Large Log Files**
   - Open tab with 100K+ lines
   - Search for text near end of file
   - Click result
   - Verify scrolling is smooth and correct

4. **Multi-line Log Entries**
   - Search for text in middle of multi-line entry
   - Click result
   - Verify entire entry is selected and visible

5. **Rapid Navigation**
   - Quickly click multiple search results
   - Verify each navigation completes correctly
   - No UI freeze or race conditions

## Future Enhancements

1. **Highlight Search Term**: Highlight the matched text within the line
2. **Previous/Next Buttons**: Navigate between matches within same tab
3. **Context Lines**: Show lines before/after match
4. **Regex Highlighting**: When regex mode is enabled, highlight all regex matches
5. **Jump to First/Last**: Quick navigation to first/last match

## Troubleshooting

### Line Not Visible After Navigation
- Check if line number is valid (0-based)
- Verify `SelectedLogLine` is being set
- Ensure `PropertyChanged` event is firing
- Check ListBox virtualization settings

### Tab Not Switching
- Verify `SelectedTabIndex` is correct
- Check if tab is in `OpenTabs` collection
- Ensure TabControl `SelectedIndex` binding is correct

### Performance Issues
- Verify virtualization is enabled
- Check if too many results are being generated
- Consider limiting results per tab (currently 5000 max)
- Profile with PerfView if needed

## Related Files

- `Xcelerator/ViewModels/LogTabViewModel.cs` - Tab view model with ScrollToLine
- `Xcelerator/ViewModels/LiveLogMonitorViewModel.cs` - Main view model with navigation logic
- `Xcelerator/Views/LiveLogMonitorView.xaml.cs` - Event handlers for double-click
- `Xcelerator/Views/LogMonitorView.xaml.cs` - Scroll handling and UI updates
- `Xcelerator/Models/LogSearchResult.cs` - Search result data model
- `Xcelerator/Models/LogSearchResultGroup.cs` - Grouped results for TreeView

## Conclusion

The search result navigation feature provides a seamless way for users to locate and view specific log entries across multiple tabs. The implementation maintains MVVM patterns, preserves performance through virtualization, and provides excellent user experience with visual feedback and smooth scrolling.
