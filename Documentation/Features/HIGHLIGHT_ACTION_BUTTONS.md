# Highlight Panel Action Buttons Feature

## Overview
Added MaterialDesign action buttons to the Highlight Panel in LogMonitorView for advanced log manipulation and navigation functionality.

## Changes Made

### 1. XAML Updates (`Xcelerator\Views\LogMonitorView.xaml`)

#### Added MaterialDesign Namespace
```xaml
xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
```

#### Added Action Buttons Section
Created a 2x3 UniformGrid containing 6 MaterialDesign flat buttons above the color swatch list:

1. **Paint/Select All** (`FormatPaint` icon)
   - Purpose: Highlight all matches with selected color
   - Click Handler: `PaintAll_Click`

2. **Unpaint/Unselect All** (`FormatClear` icon)
   - Purpose: Clear all highlights
   - Click Handler: `UnpaintAll_Click`

3. **Go Up** (`ChevronUp` icon)
   - Purpose: Navigate to previous match
   - Click Handler: `GoUp_Click`

4. **Go Down** (`ChevronDown` icon)
   - Purpose: Navigate to next match
   - Click Handler: `GoDown_Click`

5. **Collapse Logs** (`UnfoldLess` icon)
   - Purpose: Collapse matching lines in current tab
   - Click Handler: `CollapseLogs_Click`

6. **Undo Collapse** (`UnfoldMore` icon)
   - Purpose: Expand previously collapsed lines
   - Click Handler: `UndoCollapse_Click`

#### Button Specifications
- Style: `MaterialDesignFlatButton`
- Size: 28x28 pixels (compact footprint)
- Layout: UniformGrid (2 rows, 3 columns)
- Container: White border with rounded corners, padding of 8px
- Margin: 10px bottom spacing from color swatch list

### 2. Code-Behind Updates (`Xcelerator\Views\LogMonitorView.xaml.cs`)

Added 6 new event handlers with placeholder MessageBox implementations:

- `PaintAll_Click(object sender, RoutedEventArgs e)`
- `UnpaintAll_Click(object sender, RoutedEventArgs e)`
- `GoUp_Click(object sender, RoutedEventArgs e)`
- `GoDown_Click(object sender, RoutedEventArgs e)`
- `CollapseLogs_Click(object sender, RoutedEventArgs e)`
- `UndoCollapse_Click(object sender, RoutedEventArgs e)`

Each handler includes:
- DataContext validation
- TODO comments for implementation
- Placeholder MessageBox notification

## UI Layout

```
┌─────────────────────────────────────┐
│  Highlights Panel                   │
├─────────────────────────────────────┤
│  [Search Box]                       │
│                                     │
│  ┌─────────────────────────────┐   │
│  │  [🎨] [⌫] [↑]              │   │
│  │  [↓] [<<] [>>]              │   │
│  └─────────────────────────────┘   │
│                                     │
│  ○ Color 1    ✓                    │
│  ○ Color 2                         │
│  ○ Color 3                         │
└─────────────────────────────────────┘
```

## Technical Details

### Dependencies
- MaterialDesignThemes (v4.9.0) - Already installed
- MaterialDesignColors (v2.1.4) - Already installed

### Design Patterns
- Follows existing code-behind event handler pattern
- Maintains consistency with other panel controls
- Uses MaterialDesign components for modern UI

### Future Implementation Tasks

Each button currently shows a "Coming soon!" MessageBox. To fully implement:

1. **PaintAll/UnpaintAll**
   - Add methods to LogTabViewModel
   - Implement batch highlight application/removal
   - Update LogLines collection with highlight info

2. **Go Up/Down**
   - Implement search result navigation
   - Track current match position
   - Auto-scroll to match location

3. **Collapse/Expand**
   - Add filtering logic to LogLines collection
   - Implement state tracking for collapsed lines
   - Add visual indicators for collapsed state

## Benefits
- Space-efficient 28x28px buttons
- Clear iconography using MaterialDesign PackIcons
- Compact 2x3 grid layout
- Consistent with existing UI design
- Minimal impact on existing panel structure
- All buttons properly positioned within Selected Checkmark boundaries

## Testing
- Build successful: ✓
- No compilation errors: ✓
- MaterialDesign namespace properly referenced: ✓
- Event handlers properly wired: ✓

## Next Steps
1. Implement actual functionality for each button
2. Add enable/disable logic based on search/highlight state
3. Add keyboard shortcuts for common actions
4. Consider adding animation feedback on button clicks
