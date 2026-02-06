# Resource Files Fix - cluster.json and servers.json

## Problem
The application was using hard-coded absolute paths (`C:\XceleratorTool\Resources\`) to load `cluster.json` and `servers.json` files. This caused the files to not be found when the application was published and deployed to a different location without access to that directory.

## Solution
Implemented a **multi-layered solution**:
1. **Fallback path strategy** - checks application directory first, then `C:\XceleratorTool\Resources\`
2. **Diagnostic logging** - detailed logging to identify path resolution issues
3. **Automatic resource copying** - MSBuild target to copy files during publish
4. **Helper script** - PowerShell script for manual publish with resources

## Changes Made

### 1. PanelViewModel.cs
- **InitializeClusters()**: 
  - Checks `<AppDirectory>\Resources\cluster.json` first
  - Falls back to `C:\XceleratorTool\Resources\cluster.json`
  - Added comprehensive diagnostic logging
  - Shows error dialog if files not found in either location
- **LoadAndMapTopology()**: Same pattern for `servers.json`

### 2. ServerConfigManager.cs
- **GetServersJsonPath()**: Implements fallback logic with diagnostics
- **ServersJsonPath**: Property that calls the path resolution method

### 3. Xcelerator.csproj
Added two important targets:

```xml
<!-- Copy any files in project Resources folder -->
<ItemGroup>
  <None Update="Resources\**\*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>

<!-- Automatically copy files from C:\XceleratorTool\Resources during publish -->
<Target Name="CopyExternalResources" AfterTargets="Publish" Condition="Exists('C:\XceleratorTool\Resources')">
  <ItemGroup>
    <ExternalResourceFiles Include="C:\XceleratorTool\Resources\*.json" />
  </ItemGroup>
  <Copy SourceFiles="@(ExternalResourceFiles)" 
        DestinationFolder="$(PublishDir)Resources\" 
        SkipUnchangedFiles="true" 
        Condition="'@(ExternalResourceFiles)' != ''" />
  <Message Text="Copied external resource files to $(PublishDir)Resources\" 
           Importance="high" 
           Condition="'@(ExternalResourceFiles)' != ''" />
</Target>
```

### 4. publish-with-resources.ps1
Created a PowerShell helper script for publishing with automatic resource file copying.

## How to Publish

### Option 1: Automatic (Recommended) - Using dotnet publish
The `.csproj` file now includes a post-publish target that automatically copies files from `C:\XceleratorTool\Resources\` to the publish output:

```powershell
dotnet publish -c Release -r win-x64 --self-contained -o ./publish
```

The MSBuild target will automatically:
- Create `./publish/Resources/` folder
- Copy `cluster.json` and `servers.json` from `C:\XceleratorTool\Resources\`
- Show a message confirming the copy

### Option 2: Using the PowerShell Script
Run the included helper script:

```powershell
.\Xcelerator\publish-with-resources.ps1
```

Or with custom parameters:

```powershell
.\Xcelerator\publish-with-resources.ps1 -OutputPath "C:\Deploy\MyApp"
```

The script:
- Runs `dotnet publish`
- Creates the Resources folder
- Copies JSON files
- Provides detailed status messages
- Shows summary of what was copied

## How It Works

### Path Resolution Order
For both `cluster.json` and `servers.json`:
1. **First check**: `<ApplicationDirectory>\Resources\<filename>`
2. **Fallback**: `C:\XceleratorTool\Resources\<filename>`
3. **Error handling**: Shows error dialog if not found in either location

### Diagnostic Logging
All path checks are logged to Debug output:
```
[InitializeClusters] Base directory: C:\MyApp\publish\
[InitializeClusters] Checking app directory: C:\MyApp\publish\Resources\cluster.json
[InitializeClusters] Found cluster.json in app directory
```

If files aren't found, users see a clear error dialog with both paths that were checked.

## Deployment Scenarios

### Scenario 1: Development (No Changes Needed)
- Files in: `C:\XceleratorTool\Resources\`
- Application uses fallback path
- **Status**: ✅ Works immediately

### Scenario 2: Published with MSBuild Target (Recommended)
```powershell
dotnet publish -c Release -r win-x64 --self-contained -o ./publish
```
- Files automatically copied to: `./publish/Resources/`
- Application finds them in app directory
- **Status**: ✅ Fully portable, no external dependencies

### Scenario 3: Published with PowerShell Script
```powershell
.\Xcelerator\publish-with-resources.ps1
```
- Same as Scenario 2, but with better status messages
- **Status**: ✅ Fully portable with progress feedback

### Scenario 4: Deploy to Machine with C:\XceleratorTool
- Publish without bundled files
- Files remain in `C:\XceleratorTool\Resources\` on target machine
- Application uses fallback path
- **Status**: ✅ Works with existing infrastructure

## Troubleshooting Published Applications

If the published application shows an error dialog:

1. **Check the error message** - it shows both paths that were searched:
   ```
   Unable to load cluster configuration.

   Searched locations:
   1. C:\MyApp\Resources\cluster.json
   2. C:\XceleratorTool\Resources\cluster.json
   ```

2. **View diagnostic logs** - Run with a debugger or use a tool like DebugView to see:
   ```
   [InitializeClusters] Base directory: C:\MyApp\
   [InitializeClusters] Checking app directory: C:\MyApp\Resources\cluster.json
   [InitializeClusters] Not found in app directory, checking fallback: C:\XceleratorTool\Resources\cluster.json
   [InitializeClusters] ERROR: cluster.json not found in either location!
   ```

3. **Solutions**:
   - **Option A**: Ensure files exist in `<AppDirectory>\Resources\`
   - **Option B**: Ensure files exist in `C:\XceleratorTool\Resources\`
   - **Option C**: Re-publish using the MSBuild target or PowerShell script

## Testing Checklist

✅ **Development**:
- Run from Visual Studio (F5)
- Files loaded from `C:\XceleratorTool\Resources\`

✅ **Build**:
- Build the project
- Check that files in project `Resources\` folder are copied to `bin\Debug\net8.0-windows\Resources\`

✅ **Publish with MSBuild target**:
```powershell
dotnet publish -c Release -r win-x64 --self-contained -o ./publish
```
- Check for message: "Copied external resource files to ./publish/Resources/"
- Verify files exist in `./publish/Resources/`
- Run the published .exe and verify it works

✅ **Publish with PowerShell script**:
```powershell
.\Xcelerator\publish-with-resources.ps1
```
- Check for success message with checkmarks
- Verify files exist in `./publish/Resources/`
- Run the published .exe and verify it works

✅ **Portable deployment**:
- Copy published folder to a different machine
- Ensure `C:\XceleratorTool\Resources\` does NOT exist on target
- Run application - should work with bundled files

## Benefits

1. ✅ **Backward compatible**: Works with existing `C:\XceleratorTool\Resources\` setup
2. ✅ **Automatic**: MSBuild target copies files during publish
3. ✅ **Diagnostic-friendly**: Clear error messages and logging
4. ✅ **Flexible**: Multiple deployment options
5. ✅ **Portable**: Can deploy without external dependencies
6. ✅ **Developer-friendly**: No changes needed for development workflow

## Summary

**For Development**: No changes needed - keep using `C:\XceleratorTool\Resources\`

**For Publishing**: Just run:
```powershell
dotnet publish -c Release -r win-x64 --self-contained -o ./publish
```

The files will be **automatically copied** to the publish output! 🎉
