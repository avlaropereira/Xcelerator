# CRITICAL BUG FIX - Resource Files Loading Issue

## The Bug
There was a **critical logic error** in the InitializeClusters method that prevented files from being loaded:

```csharp
// WRONG - This tries to read the file when it's NULL!
if (string.IsNullOrEmpty(clusterJsonPath) || !File.Exists(clusterJsonPath))
{
    string jsonContent = File.ReadAllText(clusterJsonPath); // ❌ CRASH!
}
```

## The Fix
Fixed the logic to properly handle found vs not-found cases:

```csharp
// CORRECT - Show error when NOT found, read when found
if (string.IsNullOrEmpty(clusterJsonPath) || !File.Exists(clusterJsonPath))
{
    // Show error and return
    System.Windows.MessageBox.Show("Configuration Error...");
    return;
}

// File exists, so read it
string jsonContent = File.ReadAllText(clusterJsonPath); // ✅ SUCCESS
```

## What Was Changed

### 1. PanelViewModel.cs
- **Added `ResolveResourcePath()` helper** - Checks 4 locations:
  1. `<AppDirectory>\Resources\filename`
  2. `<ExecutableDirectory>\Resources\filename`
  3. `<AppDirectory>\filename`
  4. `C:\XceleratorTool\Resources\filename` (fallback)

- **Fixed `InitializeClusters()`** - Now properly:
  - Returns early with error if file not found
  - Reads file only when it exists

- **Fixed `LoadAndMapTopology()`** - Uses the same robust path resolution

### 2. ServerConfigManager.cs
- **Updated `GetServersJsonPath()`** - Same 4-location check

### 3. Xcelerator.csproj
- **Added MSBuild target** `CopyExternalResources`:
  - Automatically runs after Publish
  - Copies `*.json` from `C:\XceleratorTool\Resources\` to publish output
  - No manual steps needed!

## How to Publish

Simply run:
```powershell
dotnet publish -c Release -r win-x64 --self-contained -o ./publish
```

The MSBuild target will automatically copy the JSON files to `./publish/Resources/`

## Verification

After publishing, verify the files exist:
```powershell
dir ./publish/Resources/
```

You should see:
- cluster.json
- servers.json
- ColorConfig.json

## Testing

Run the published application:
```powershell
./publish/Xcelerator.exe
```

The application will now:
1. Check `./publish/Resources/` first
2. Fall back to `C:\XceleratorTool\Resources\` if not found
3. Show clear error if files missing from both locations

## Path Resolution Order

The application checks these locations in order:

### For cluster.json and servers.json:
1. `<ApplicationDirectory>\Resources\cluster.json`
2. `<ExecutableLocation>\Resources\cluster.json`
3. `<ApplicationDirectory>\cluster.json`
4. `C:\XceleratorTool\Resources\cluster.json`

First file found is used. If none found, shows error dialog.

## What You Get

✅ **Automatic file copying** - MSBuild handles it during publish  
✅ **Robust path resolution** - Checks multiple locations  
✅ **Clear error messages** - Know exactly what's wrong  
✅ **Diagnostic logging** - See all paths checked in Debug output  
✅ **Backward compatible** - Still works with `C:\XceleratorTool\Resources\`  
✅ **Fully portable** - Can deploy without external dependencies  

## Deployment Options

### Option 1: Portable (Recommended)
Publish normally - files are auto-copied to output:
```powershell
dotnet publish -c Release -r win-x64 --self-contained -o ./publish
```
Deploy the entire `./publish` folder. Works anywhere!

### Option 2: Shared Resources
Publish and deploy, but keep files in `C:\XceleratorTool\Resources\` on target machines.
Application will find them via fallback.

## Troubleshooting

### Problem: Application shows "Configuration Error"

**Solution**: The error dialog shows which locations were checked. Either:
1. Ensure files are in `<AppDirectory>\Resources\`
2. OR ensure files are in `C:\XceleratorTool\Resources\`

### Problem: MSBuild target didn't copy files

**Check**: 
```powershell
Test-Path "C:\XceleratorTool\Resources\cluster.json"
Test-Path "C:\XceleratorTool\Resources\servers.json"
```

If files don't exist there, put them there first, then republish.

### Problem: Files copied but app still doesn't load them

**Check Debug Output**: The app logs every path it checks:
```
[ResolveResourcePath] Looking for: cluster.json
[ResolveResourcePath] Checking: C:\MyApp\Resources\cluster.json
[ResolveResourcePath] Found at: C:\MyApp\Resources\cluster.json
```

Use Visual Studio's Output window or a tool like [DebugView](https://learn.microsoft.com/en-us/sysinternals/downloads/debugview) to see these logs.

## Summary

**The Issue**: Logic bug prevented reading files even when found  
**The Fix**: Corrected logic + robust multi-location path resolution  
**The Result**: Published apps now work correctly with bundled files  

**Status**: ✅ FIXED AND TESTED
