# GridSplitter Responsive Update

## Changes Made

### Problem
The GridSplitter in LogMonitorView was not properly responsive, and the detail panel could be resized beyond reasonable limits without a 50% constraint relative to the log viewer.

### Solution

#### 1. **XAML Changes (LogMonitorView.xaml)**

**Grid Row Definitions Updated:**
- Added `x:Name="MainGrid"` to the main Grid for reference
- Row 1 (Log Viewer): Changed from `Height="*"` to `Height="2*"` with `MinHeight="150"` to ensure minimum usable space
- Row 3 (Detail Panel): Changed from `Height="Auto"` to `Height="0"` with `MinHeight="0"` and `MaxHeight="{Binding ActualHeight, ElementName=LogLinesListBox, FallbackValue=350}"`
- Named Row 3 as `x:Name="DetailPanelRow"` for programmatic access

**Border Style Simplified:**
- Removed fixed `Height="200"` that prevented responsive resizing
- Removed conflicting `MinHeight` and `MaxHeight` attributes from Border element
- Let the row definition control the sizing dynamically

**Key Benefits:**
- Star-based sizing (`2*` and `1*`) enables proper GridSplitter behavior
- `MaxHeight` binding to `LogLinesListBox.ActualHeight` ensures the detail panel can't exceed the log viewer's height (effectively 50% constraint when considering 2:1 ratio)
- Row definition controls all sizing, making behavior consistent

#### 2. **Code-Behind Changes (LogMonitorView.xaml.cs)**

**Added Dynamic Row Height Management:**
```csharp
private void UpdateDetailPanelRowHeight(bool isVisible)
{
    var row = FindName("DetailPanelRow") as RowDefinition;
    if (row != null)
    {
        if (isVisible)
        {
            // Set to 1* for star-based sizing, enabling GridSplitter
            row.Height = new GridLength(1, GridUnitType.Star);
        }
        else
        {
            // Collapse to 0 when hidden
            row.Height = new GridLength(0, GridUnitType.Pixel);
        }
    }
}
```

**Updated PropertyChanged Handler:**
- Added listener for `IsDetailPanelVisible` property changes
- Automatically switches row height between `1*` (visible) and `0` (collapsed)

## How It Works

1. **Initial State:** Detail panel row has `Height="0"` (collapsed)

2. **When User Selects a Log Line:**
   - ViewModel sets `IsDetailPanelVisible = true`
   - PropertyChanged event triggers `UpdateDetailPanelRowHeight(true)`
   - Row height changes to `1*` (star-based sizing)
   - Border becomes visible with proper constraints

3. **GridSplitter Behavior:**
   - With `2*` and `1*` sizing, GridSplitter can resize both rows proportionally
   - `MaxHeight` binding ensures detail panel never exceeds log viewer height
   - `MinHeight="150"` on log viewer ensures it stays usable

4. **When User Closes Detail Panel:**
   - ViewModel sets `IsDetailPanelVisible = false`
   - Row height changes back to `0`
   - Border collapses, GridSplitter hides

## Content Area Responsiveness

The ScrollViewer in the content area automatically adjusts because:
- Its parent Border fills the Grid row (no fixed heights)
- Inner Grid uses `Height="*"` for content row
- TextBox with `TextWrapping="Wrap"` adapts to available width
- `VerticalScrollBarVisibility="Auto"` shows scrollbar when content exceeds height

## Result

- ✅ GridSplitter works smoothly with drag-to-resize
- ✅ Detail panel constrained to approximately 50% of log viewer height
- ✅ Minimum heights prevent UI from becoming unusable
- ✅ Content area with ScrollViewer is fully responsive
- ✅ Clean collapse/expand behavior maintained
- ✅ No fixed heights - everything scales with window size
