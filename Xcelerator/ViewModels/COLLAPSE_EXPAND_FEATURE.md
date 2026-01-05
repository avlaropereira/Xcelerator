# Collapse/Expand Search Results Feature

## Overview

Added interactive collapse/expand functionality for grouped search results with multiple interaction methods for better user experience.

## Features Implemented

### 1. **Collapse Button** ?
- Visible only when group is expanded
- Located on the right side of the group header
- Single-click to collapse the group
- Shows "Collapse" text with blue border
- Hover effect for visual feedback

### 2. **Chevron Icon Toggle** ?
- Interactive button on the left of group header
- Click to toggle between expanded/collapsed
- Changes icon:
  - `ChevronRight` (?) when collapsed
  - `ChevronDown` (?) when expanded
- Visual indicator of current state

### 3. **Double-Click Tab Name** ?
- Double-click the tab name text to toggle expansion
- Quick keyboard-free interaction
- Natural UI pattern
- Prevents accidental toggles (requires double-click)

## User Interface

### Group Header Layout

```
????????????????????????????????????????????????????
? [?] Tab Name (15 matches)        [Collapse]     ?
?     ?? Line 123: Log entry preview...            ?
?     ?? Line 456: Another log entry...            ?
?     ?? Line 789: More log data...                ?
????????????????????????????????????????????????????

When collapsed:
????????????????????????????????????????????????????
? [?] Tab Name (15 matches)                        ?
????????????????????????????????????????????????????
```

### Interaction Methods

| Action | Result | Use Case |
|--------|--------|----------|
| **Click chevron icon** | Toggle expand/collapse | Quick single action |
| **Double-click tab name** | Toggle expand/collapse | Keyboard-free workflow |
| **Click "Collapse" button** | Collapse group | Explicit collapse action |

## Technical Implementation

### XAML Changes

#### Group Header Template

```xaml
<HierarchicalDataTemplate DataType="{x:Type models:LogSearchResultGroup}">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>      <!-- Chevron button -->
            <ColumnDefinition Width="*"/>         <!-- Tab name -->
            <ColumnDefinition Width="Auto"/>      <!-- Collapse button -->
        </Grid.ColumnDefinitions>
        
        <!-- 1. Chevron Toggle Button -->
        <Button Grid.Column="0"
                Click="ToggleGroupExpansion_Click">
            <materialDesign:PackIcon Kind="ChevronRight/Down"/>
        </Button>
        
        <!-- 2. Double-Click Tab Name -->
        <TextBlock Grid.Column="1"
                   Text="{Binding DisplayText}"
                   MouseLeftButtonDown="GroupHeader_MouseLeftButtonDown"/>
        
        <!-- 3. Collapse Button (visible when expanded) -->
        <Button Grid.Column="2"
                Content="Collapse"
                Click="CollapseGroup_Click"
                Visibility="{Binding IsExpanded, Converter={...}}"/>
    </Grid>
</HierarchicalDataTemplate>
```

### Code-Behind Event Handlers

#### 1. Collapse Button Handler
```csharp
private void CollapseGroup_Click(object sender, RoutedEventArgs e)
{
    if (sender is Button button && button.DataContext is LogSearchResultGroup group)
    {
        group.IsExpanded = false;  // Always collapse
    }
}
```

**Purpose:** Explicit collapse action only (one-directional)

#### 2. Chevron Toggle Handler
```csharp
private void ToggleGroupExpansion_Click(object sender, RoutedEventArgs e)
{
    if (sender is Button button && button.DataContext is LogSearchResultGroup group)
    {
        group.IsExpanded = !group.IsExpanded;  // Toggle both ways
    }
}
```

**Purpose:** Bi-directional toggle (expand ? collapse)

#### 3. Double-Click Handler
```csharp
private void GroupHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    if (e.ClickCount == 2 && sender is TextBlock textBlock && 
        textBlock.DataContext is LogSearchResultGroup group)
    {
        group.IsExpanded = !group.IsExpanded;  // Toggle on double-click
        e.Handled = true;
    }
}
```

**Purpose:** Quick toggle via double-click, prevents accidental toggles

## User Experience

### Workflow Examples

#### Scenario 1: Quick Collapse Single Group
```
User: Sees expanded group with many results
User: Clicks "Collapse" button
Result: Group collapses instantly
```

#### Scenario 2: Toggle Multiple Groups
```
User: Has 5 expanded groups
User: Clicks chevron icon on each
Result: Each group toggles (expands/collapses)
```

#### Scenario 3: Keyboard-Free Navigation
```
User: Browsing results
User: Double-clicks tab names to toggle
Result: Quick expand/collapse without reaching for mouse button
```

### Visual Feedback

#### Chevron Icon States
- **Collapsed:** `?` ChevronRight (points right)
- **Expanded:** `?` ChevronDown (points down)

#### Collapse Button Visibility
- **Expanded:** Visible with blue border
- **Collapsed:** Hidden (no clutter)

#### Hover Effects
- **Chevron button:** No visual change (icon is indicator)
- **Tab name:** Hand cursor indicates clickable
- **Collapse button:** Light blue background (#FFE3F2FD)
- **Collapse button pressed:** Darker blue (#FFBBDEFB)

## Benefits

### 1. **Multiple Interaction Options**
Users can choose their preferred method:
- Visual explicit button (Collapse)
- Icon toggle (Chevron)
- Quick gesture (Double-click)

### 2. **Intuitive Design**
- Chevron direction indicates state
- Collapse button only shown when relevant
- Double-click is a natural UI pattern

### 3. **Improved Navigation**
- Quickly hide irrelevant results
- Focus on specific tabs
- Reduce visual clutter

### 4. **No Accidental Actions**
- Collapse button requires deliberate click
- Double-click prevents single-click accidents
- Chevron is clearly clickable button

## Accessibility

### Keyboard Support
While current implementation is mouse-based, future enhancement could add:
```csharp
// Future: Keyboard support
PreviewKeyDown += (s, e) =>
{
    if (e.Key == Key.Space || e.Key == Key.Enter)
    {
        group.IsExpanded = !group.IsExpanded;
    }
};
```

### Screen Reader Support
```xaml
<!-- Future: Accessibility improvements -->
<Button AutomationProperties.Name="{Binding DisplayText}"
        AutomationProperties.HelpText="Toggle group expansion"/>
```

## Testing Recommendations

### 1. Interaction Testing
```
? Click chevron icon - should toggle
? Click "Collapse" button - should collapse
? Double-click tab name - should toggle
? Single-click tab name - should NOT toggle
```

### 2. State Persistence
```
? Collapsed state persists when scrolling
? New searches respect default expanded state
? IsExpanded binding works two-way
```

### 3. Visual Testing
```
? Chevron icon changes correctly
? Collapse button appears/disappears
? Hover effects work properly
? Layout doesn't shift when collapsing
```

### 4. Edge Cases
```
? Rapidly clicking chevron - no double toggle
? Clicking during collapse animation - works correctly
? Multiple groups can be collapsed independently
```

## Future Enhancements

### 1. Collapse All Button
Add a global button to collapse all groups:
```xaml
<Button Content="Collapse All" Click="CollapseAll_Click"/>
```

```csharp
private void CollapseAll_Click(object sender, RoutedEventArgs e)
{
    if (DataContext is LiveLogMonitorViewModel viewModel)
    {
        foreach (var group in viewModel.SearchResultGroups)
        {
            group.IsExpanded = false;
        }
    }
}
```

### 2. Expand All Button
```xaml
<Button Content="Expand All" Click="ExpandAll_Click"/>
```

### 3. Remember User Preferences
Save collapse/expand state per tab:
```csharp
private Dictionary<string, bool> _groupStates = new();

private void SaveGroupState(LogSearchResultGroup group)
{
    _groupStates[group.TabName] = group.IsExpanded;
}
```

### 4. Animation
Add smooth collapse/expand animation:
```xaml
<Style.Triggers>
    <Trigger Property="IsExpanded" Value="True">
        <Trigger.EnterActions>
            <BeginStoryboard>
                <Storyboard>
                    <DoubleAnimation Duration="0:0:0.2"/>
                </Storyboard>
            </BeginStoryboard>
        </Trigger.EnterActions>
    </Trigger>
</Style.Triggers>
```

### 5. Keyboard Shortcuts
Add keyboard shortcuts:
- `Space` - Toggle selected group
- `Ctrl+E` - Expand all
- `Ctrl+Shift+E` - Collapse all

## Configuration

### Adjust Collapse Button Style

Modify colors in XAML:
```xaml
<Button BorderBrush="#FF0078D4"      <!-- Border color -->
        Foreground="#FF0078D4"       <!-- Text color -->
        Background="Transparent">    <!-- Background -->
```

### Adjust Hover Colors
```xaml
<Trigger Property="IsMouseOver" Value="True">
    <Setter Property="Background" Value="#FFE3F2FD"/>  <!-- Light blue -->
</Trigger>
```

### Change Default Expansion State

In `LogSearchResultGroup` model:
```csharp
private bool _isExpanded = false;  // Change to false for default collapsed
```

Or in ViewModel when creating groups:
```csharp
var group = new LogSearchResultGroup
{
    TabName = tab.HeaderName,
    IsExpanded = false  // Start collapsed
};
```

## Conclusion

The collapse/expand feature provides flexible, intuitive interaction methods for managing grouped search results:

? **Three interaction methods** - Chevron button, collapse button, double-click  
? **Visual feedback** - Icons change, buttons respond to hover  
? **No accidental actions** - Deliberate interactions required  
? **Clean UI** - Collapse button hidden when not needed  
? **Natural patterns** - Double-click and icons are familiar to users  

Users can now efficiently navigate large result sets by collapsing irrelevant groups and focusing on specific tabs.
