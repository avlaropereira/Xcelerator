# Auto-Refresh Dropdown Smart Filtering Feature

## Overview
Enhanced the Auto-Refresh Interval dropdown to be intelligent about when it's available and which intervals it shows based on the actual time it took to load the logs.

## Changes Made

### 1. **LogTabViewModel.cs** - Backend Logic
Added new properties and logic to track load time and control dropdown availability:

- **`LoadTimeSeconds`**: Tracks how long it took to load the logs (in seconds)
- **`IsRefreshDropdownEnabled`**: Controls whether the dropdown is enabled
  - Starts as `false` (disabled) when loading begins
  - Set to `true` after logs finish loading
- **`MinimumRefreshIntervalMinutes`**: Calculated property that determines the minimum acceptable refresh interval
  - Formula: `Ceiling(LoadTimeSeconds / 60) + 1`
  - Example: If logs take 2.3 minutes to load → minimum is 3 minutes

#### Load Time Tracking
- Stopwatch tracks the entire load operation (including retries)
- `LoadTimeSeconds` is set in the `finally` block to ensure it captures the total time
- Dropdown is enabled once loading completes (success or failure)

### 2. **RefreshIntervalItemVisibilityConverter.cs** - New Converter
Created a multi-value converter that determines which ComboBox items should be visible:

- Takes two inputs:
  1. The item's tag value (the minute value)
  2. The `LoadTimeSeconds` from the ViewModel
- Returns `Visibility.Visible` for items that are:
  - "Off" (value = 0), always available
  - Greater than or equal to the calculated minimum threshold
- Returns `Visibility.Collapsed` for items below the threshold

### 3. **LogMonitorView.xaml** - UI Updates
Updated the Auto-Refresh ComboBox to use the new functionality:

- Added `IsEnabled="{Binding IsRefreshDropdownEnabled}"` to disable dropdown while loading
- Registered the new `RefreshIntervalItemVisibilityConverter` in Resources
- Applied the converter to each `ComboBoxItem` via `ItemContainerStyle`:
  - Uses `MultiBinding` to bind both the item's `Tag` and the ViewModel's `LoadTimeSeconds`
  - Each item's visibility is calculated dynamically based on the load time

## Behavior Examples

| Load Time | Minimum Interval | Available Options |
|-----------|-----------------|-------------------|
| 30 seconds | 2 minutes | Off, 2 min, 3 min, 4 min, 5 min, 7 min |
| 1.5 minutes | 3 minutes | Off, 3 min, 4 min, 5 min, 7 min |
| 2.8 minutes | 4 minutes | Off, 4 min, 5 min, 7 min |
| 5.1 minutes | 7 minutes | Off, 7 min |
| 8 minutes | 9 minutes | Off (only) |

## User Experience
1. **Before logs load**: Dropdown is disabled (grayed out)
2. **During loading**: User cannot interact with the dropdown
3. **After logs load**: Dropdown becomes enabled and shows only appropriate intervals
4. **Smart filtering**: Only shows intervals that are longer than the load time + 1 minute buffer

## Technical Benefits
- **Prevents system overload**: Users can't set refresh intervals shorter than load time
- **Clear UX**: Disabled state clearly indicates when feature is not ready
- **Dynamic adaptation**: Works regardless of log file size or network speed
- **Safety buffer**: The "+1 minute" ensures there's always breathing room between refreshes
