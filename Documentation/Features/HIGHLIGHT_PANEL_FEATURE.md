# Highlight Panel Feature

## Overview
The Highlight Panel is a color-coded filtering feature for the LogMonitorView that allows users to select predefined color highlights for log entries. This feature follows modern Fluent/Material design principles with rounded corners, subtle shadows, and smooth hover states.

## Architecture

### Data Model
- **HighlightSetting.cs**: Core model representing a single highlight setting
  - Uses `System.Windows.Media.Color` for properties (BackColor, BorderColor, MarkerColor)
  - Implements `INotifyPropertyChanged` for reactive UI updates
  - Provides frozen `SolidColorBrush` objects for performance optimization
  - Includes static method `IntToColor()` to convert signed ARGB integers to Color objects

- **HighlightSettingContainer.cs**: XML deserialization container
  - Contains `HighlightSettingXml` for parsing signed integer color values
  - Provides `ToHighlightSetting()` method to convert XML data to domain model

### ViewModel Integration
**LogTabViewModel.cs** has been extended with:
- `HighlightSettings` - ObservableCollection of available highlights
- `IsHighlightPanelVisible` - Visibility state of the panel
- `SelectedHighlight` - Currently selected highlight setting
- `LoadHighlightSettings()` - Loads settings from embedded XML
- `UpdateHighlightSelection()` - Manages single-selection behavior
- `ToggleHighlightPanel()` - Toggles panel visibility

### View Implementation
**LogMonitorView.xaml** features:
- Three-column grid layout (logs, splitter, highlights)
- Modern card-style highlight swatches with:
  - 56x56px rounded corners (6px radius)
  - Drop shadow effects for depth
  - Enhanced hover state with increased shadow
  - Checkmark overlay for selected state
  - WrapPanel layout for responsive arrangement
- Vertical GridSplitter for resizable panel
- Toggle button in status bar with palette icon

### Converter
**HighlightSelectedToVisibilityConverter.cs**: Converts IsSelected boolean to Visibility for checkmark display

## Performance Optimizations

### 1. Frozen Brushes
All color brushes are created as frozen (`Freeze()` called):
```csharp
public SolidColorBrush BackgroundBrush
{
    get
    {
        var brush = new SolidColorBrush(BackColor);
        brush.Freeze();
        return brush;
    }
}
```
**Benefit**: Frozen brushes are read-only and thread-safe, reducing WPF composition engine overhead.

### 2. Color Conversion at Deserialization
Colors are converted from signed integers during initial XML parsing:
```csharp
public static Color IntToColor(int argb)
{
    byte a = (byte)((argb >> 24) & 0xFF);
    byte r = (byte)((argb >> 16) & 0xFF);
    byte g = (byte)((argb >> 8) & 0xFF);
    byte b = (byte)(argb & 0xFF);
    return Color.FromArgb(a, r, g, b);
}
```
**Benefit**: Conversion happens once, not during each scroll/render cycle.

### 3. Virtualization-Friendly
The highlight panel is separate from the virtualized log list, ensuring:
- No impact on ListBox virtualization behavior
- Minimal UI thread blocking
- No layout pass triggers during log scrolling

## Design Principles

### Modern UI Elements
1. **Card Design**: Each swatch is a card with rounded corners and shadow
2. **Hover States**: Enhanced shadow on mouse over for interactivity feedback
3. **Selection Indicator**: White circle with checkmark using Segoe MDL2 Assets font
4. **Color Harmony**: Border colors match marker colors for visual consistency

### Layout Strategy
- **Primary Content**: Logs occupy main area (Grid Column 0)
- **Secondary Panel**: Highlights in sidebar (Grid Column 2)
- **Resizable**: GridSplitter allows user-controlled width
- **Collapsible**: Panel can be hidden to maximize log viewing area

## Usage

### Toggle Panel
Click the palette icon (?) in the status bar to show/hide the highlight panel.

### Select Highlight
Click any color swatch to select it. Only one highlight can be active at a time.

### Resize Panel
Drag the vertical splitter between logs and highlights to adjust panel width.

## Color Settings Format

The highlight colors are loaded from XML with signed integer ARGB values:

```xml
<HighlightSetting>
  <BackColor>-7876870</BackColor>
  <BorderColor>-7876885</BorderColor>
  <MarkerColor>-7876885</MarkerColor>
  <Flags>5</Flags>
</HighlightSetting>
```

**Conversion Example**:
- Signed int: `-7876870` → Hex: `0xFF87CEEB` → Color: SkyBlue (A=255, R=135, G=206, B=235)

## Future Enhancements
1. Apply selected highlight to filter/emphasize matching log lines
2. Allow custom highlight creation and persistence
3. Search/filter integration with highlight selection
4. Keyboard shortcuts for highlight selection (Ctrl+1-9)
5. Export/import custom highlight schemes
