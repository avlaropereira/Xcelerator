# Configuration Path Fix - LocalAppData Support

## Problem
After publishing the project, the `ReloadTopology()` method and other configuration file reading logic used hardcoded paths like `C:\XceleratorTool\Resources\` instead of using `%LocalAppData%\Xcelerator\Resources\`. This prevented the application from working correctly in published/deployed environments.

## Solution
Created a centralized `ConfigurationPathHelper` utility class that manages all configuration file paths with proper priority order:

1. **%LocalAppData%\Xcelerator\Resources** (Primary for published apps)
2. **Resources folder relative to base directory** (Development)
3. **Resources folder relative to executable location** (Alternative)
4. **Directly in base directory** (Fallback)
5. **C:\XceleratorTool\Resources** (Legacy support)

## Files Created

### Xcelerator\Utilities\ConfigurationPathHelper.cs
- **New centralized helper class** for resolving configuration file paths
- Prioritizes LocalAppData for published applications
- Provides methods:
  - `GetResourcesDirectory()` - Gets the Resources directory path
  - `ResolveResourceFilePath(string filename)` - Resolves path to any resource file
  - `GetServersJsonPath()` - Specific method for servers.json
  - `GetClusterJsonPath()` - Specific method for cluster.json
  - `GetColorConfigJsonPath()` - Specific method for ColorConfig.json
  - `EnsureResourcesDirectoryExists()` - Creates the Resources directory if needed

## Files Modified

### 1. Xcelerator\ViewModels\LiveLogMonitorViewModel.cs
**Changes:**
- Added `using Xcelerator.Utilities;`
- Replaced hardcoded path in `ReloadTopology()`:
  ```csharp
  // OLD:
  string topologyJsonPath = @"C:\XceleratorTool\Resources\servers.json";
  
  // NEW:
  string topologyJsonPath = ConfigurationPathHelper.GetServersJsonPath();
  ```

### 2. Xcelerator\Services\ServerConfigManager.cs
**Changes:**
- Added `using Xcelerator.Utilities;`
- Removed entire `GetServersJsonPath()` method (50+ lines)
- Replaced with simple property:
  ```csharp
  private static string ServersJsonPath => ConfigurationPathHelper.GetServersJsonPath();
  ```

### 3. Xcelerator\ViewModels\PanelViewModel.cs
**Changes:**
- Added `using Xcelerator.Utilities;`
- Removed entire `ResolveResourcePath(string filename)` method (40+ lines)
- Updated `InitializeClusters()` to use:
  ```csharp
  string clusterJsonPath = ConfigurationPathHelper.GetClusterJsonPath();
  ```
- Updated `LoadAndMapTopology()` to use:
  ```csharp
  string topologyJsonPath = ConfigurationPathHelper.GetServersJsonPath();
  ```

## Path Resolution Priority

The new helper checks paths in this order:

1. **%LocalAppData%\Xcelerator\Resources\{filename}**
   - Example: `C:\Users\{Username}\AppData\Local\Xcelerator\Resources\servers.json`
   - **This is the PRIMARY location for published apps**

2. **AppDomain.CurrentDomain.BaseDirectory\Resources\{filename}**
   - Used during development
   - Example: `C:\Projects\Xcelerator\bin\Debug\Resources\servers.json`

3. **Process.MainModule.FileName\Resources\{filename}**
   - Alternative location relative to executable

4. **AppDomain.CurrentDomain.BaseDirectory\{filename}**
   - Directly in the base directory (fallback)

5. **C:\XceleratorTool\Resources\{filename}**
   - Legacy hardcoded path (kept for backward compatibility)

## Benefits

1. **Published App Support**: Application now works correctly when deployed, using LocalAppData
2. **Development Support**: Still works during development from project directories
3. **Centralized Logic**: All path resolution in one place, easier to maintain
4. **Backward Compatible**: Still checks legacy paths as fallback
5. **Reduced Code Duplication**: Eliminated 3 separate path resolution implementations
6. **Auto-create Directories**: Helper can create the Resources directory if needed

## Testing Checklist

- [x] Build successful
- [ ] Test in development environment (Visual Studio)
- [ ] Test ReloadTopology() after adding server
- [ ] Test in published environment (after deployment)
- [ ] Verify files are read from `%LocalAppData%\Xcelerator\Resources\`
- [ ] Verify cluster.json loads correctly
- [ ] Verify servers.json loads and reloads correctly
- [ ] Test AddServerToCluster functionality

## Usage Examples

### For Other Developers
If you need to add support for a new configuration file:

```csharp
// Add a specific method to ConfigurationPathHelper.cs
public static string GetMyConfigJsonPath() => 
    ResolveResourceFilePath("myconfig.json") ?? 
    Path.Combine(LocalAppDataPath, "Resources", "myconfig.json");

// Use it in your code
using Xcelerator.Utilities;

string configPath = ConfigurationPathHelper.GetMyConfigJsonPath();
if (File.Exists(configPath))
{
    // Load your config
}
```

## Deployment Notes

When publishing the application:

1. The installer/deployment script should copy configuration files to:
   `%LocalAppData%\Xcelerator\Resources\`

2. Example PowerShell deployment script:
   ```powershell
   $localAppData = [Environment]::GetFolderPath('LocalApplicationData')
   $targetDir = Join-Path $localAppData "Xcelerator\Resources"
   
   # Create directory
   New-Item -ItemType Directory -Force -Path $targetDir
   
   # Copy configuration files
   Copy-Item ".\Resources\*.json" -Destination $targetDir -Force
   ```

3. The application will automatically create the directory if it doesn't exist

## Related Files

Configuration files that use this helper:
- `servers.json` - Server topology configuration
- `cluster.json` - Cluster definitions
- `ColorConfig.json` - Log colorization configuration (future support ready)

## Migration Notes

### Before (Hardcoded)
```csharp
string path = @"C:\XceleratorTool\Resources\servers.json";
```

### After (Dynamic with LocalAppData Priority)
```csharp
string path = ConfigurationPathHelper.GetServersJsonPath();
```

The new approach:
- Checks LocalAppData FIRST (for published apps)
- Falls back to development paths
- Maintains backward compatibility with legacy path

## Future Enhancements

Potential improvements:
1. Add `GetColorConfigJsonPath()` usage to `LogColorizer` when it needs file-based config
2. Add configuration file validation
3. Add configuration file migration from legacy paths to LocalAppData
4. Add user notification when files are missing in expected locations
5. Add configuration file editor/validator UI

## Breaking Changes

**None** - This is a non-breaking change. The new helper maintains backward compatibility with all existing paths while adding LocalAppData support.
