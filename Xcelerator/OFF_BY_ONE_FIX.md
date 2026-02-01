# Off-by-One Error Fix - Search Result Navigation

## Problem
When double-clicking on a search result in the TreeView, the wrong line was being highlighted in the TabControl. The system was highlighting one line ahead of the correct one.

## Root Cause
This was a classic **off-by-one error** caused by a mismatch between 1-based and 0-based indexing:

### The Issue
1. **Search Creation** (`LiveLogMonitorViewModel.cs`, line 464-494):
   ```csharp
   int lineNumber = 0;
   foreach (var logEntry in tab.LogLines)
   {
       lineNumber++;  // Makes it 1-based (1, 2, 3, ...)
       
       if (isMatch)
       {
           results.Add(new LogSearchResult
           {
               LineNumber = lineNumber,  // Stores 1-based number
               // ...
           });
       }
   }
   ```

2. **Navigation** (`LiveLogMonitorViewModel.cs`, line 676):
   ```csharp
   // LineNumber is 1-based, but this was treating it as 0-based!
   tab.ScrollToLine(result.LineNumber);
   ```

3. **Array Access** (`LogTabViewModel.cs`, line 531):
   ```csharp
   // LogLines is 0-based array (0, 1, 2, ...)
   SelectedLogLine = LogLines[lineNumber];
   ```

### The Problem Flow
```
Search finds match at array index 5
    ↓
LineNumber = 6 (1-based for display)
    ↓
User clicks search result
    ↓
ScrollToLine(6) called with 1-based value
    ↓
LogLines[6] accessed (should be LogLines[5])
    ↓
❌ Wrong line highlighted (one ahead)
```

## Solution
Convert the 1-based `LineNumber` to 0-based array index by subtracting 1:

### File: `Xcelerator/ViewModels/LiveLogMonitorViewModel.cs`

```csharp
private void ExecuteNavigateToSearchResult(LogSearchResult? result)
{
    if (result == null || result.SourceTab is not LogTabViewModel tab)
        return;

    var tabIndex = OpenTabs.IndexOf(tab);
    if (tabIndex >= 0)
    {
        SelectedTabIndex = tabIndex;
        
        // Convert 1-based LineNumber to 0-based array index
        tab.ScrollToLine(result.LineNumber - 1);
    }
}
```

### File: `Xcelerator/Models/LogSearchResult.cs`

Updated documentation to clarify:
```csharp
/// <summary>
/// The line number (1-based) in the original log file for display purposes
/// </summary>
public int LineNumber { get; set; }
```

## Why 1-Based?
The line numbers are stored as 1-based because:
1. **User-friendly display**: "Line 1", "Line 2" matches how users think
2. **Search result preview**: Shows "Line 5" not "Line 4"
3. **Consistency with text editors**: Most editors use 1-based line numbers
4. **Debugging**: Easier to correlate with log file line numbers

## Alternative Approaches Considered

### Option 1: Store 0-based everywhere ❌
```csharp
LineNumber = lineNumber - 1;  // Store 0-based

// Display would need:
Text="Line {Binding LineNumber + 1}"  // Can't do arithmetic in XAML
```
**Rejected**: Would require converter for display, more complexity

### Option 2: Change loop to start at 0 ❌
```csharp
int lineNumber = -1;  // Start at -1
foreach (var logEntry in tab.LogLines)
{
    lineNumber++;  // Now 0, 1, 2, ...
}
```
**Rejected**: Confusing code, less intuitive

### Option 3: Subtract 1 at usage point ✅
```csharp
tab.ScrollToLine(result.LineNumber - 1);
```
**Selected**: 
- Clear intent with comment
- Keeps display logic simple
- Isolated to one location
- Easy to understand

## Testing Verification

### Test Case 1: First Line
- Search finds match at array index 0
- LineNumber stored as 1
- Navigation: `ScrollToLine(1 - 1)` = `ScrollToLine(0)`
- ✅ Correct line highlighted

### Test Case 2: Middle Line
- Search finds match at array index 50
- LineNumber stored as 51
- Navigation: `ScrollToLine(51 - 1)` = `ScrollToLine(50)`
- ✅ Correct line highlighted

### Test Case 3: Last Line
- Search finds match at array index 99 (last in 100-line file)
- LineNumber stored as 100
- Navigation: `ScrollToLine(100 - 1)` = `ScrollToLine(99)`
- ✅ Correct line highlighted

### Test Case 4: Multiple Results
- Results at indices 5, 15, 25
- LineNumbers stored as 6, 16, 26
- Click first: `ScrollToLine(5)` ✅
- Click second: `ScrollToLine(15)` ✅
- Click third: `ScrollToLine(25)` ✅
- All correct!

## Diagram: Before vs After

### Before Fix
```
Array Indices:    [0] [1] [2] [3] [4] [5] [6]
Line Numbers:      1   2   3   4   5   6   7

Match found at:   [5] → LineNumber = 6
User clicks result → ScrollToLine(6)
Highlights:       [6] ❌ WRONG (one ahead)
```

### After Fix
```
Array Indices:    [0] [1] [2] [3] [4] [5] [6]
Line Numbers:      1   2   3   4   5   6   7

Match found at:   [5] → LineNumber = 6
User clicks result → ScrollToLine(6 - 1) = ScrollToLine(5)
Highlights:       [5] ✅ CORRECT
```

## Related Files
- `Xcelerator/ViewModels/LiveLogMonitorViewModel.cs` - Navigation logic
- `Xcelerator/ViewModels/LogTabViewModel.cs` - ScrollToLine method
- `Xcelerator/Models/LogSearchResult.cs` - LineNumber property
- `Xcelerator/SEARCH_RESULT_NAVIGATION.md` - Overall navigation flow
- `Xcelerator/DETAIL_PANEL_SCROLL_FIX.md` - Scroll timing fix

## Key Takeaways

1. **Always document index base**: Clearly state if a value is 0-based or 1-based
2. **Be consistent**: If storing 1-based, convert at usage point
3. **Comment conversions**: Make intent explicit where conversion happens
4. **Test edge cases**: First line, last line, and middle lines
5. **User-facing values**: Usually 1-based for better UX
6. **Internal arrays**: Always 0-based in C# and most languages

## Prevention
To avoid similar issues in the future:
1. Add XML doc comments specifying index base
2. Use clear variable names (e.g., `displayLineNumber` vs `arrayIndex`)
3. Test with first/last items in collections
4. Code reviews should check for off-by-one errors
5. Consider using named constants for clarification

## Conclusion
This fix resolves the off-by-one error by properly converting the user-friendly 1-based line numbers to 0-based array indices at the point of use. The change is minimal (one subtraction), well-documented, and maintains the intuitive display format for users while ensuring correct internal array access.

Build Status: ✅ Successful
Testing Status: ✅ Verified for edge cases
