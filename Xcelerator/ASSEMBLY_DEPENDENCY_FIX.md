# Assembly Dependency Fix - System.Diagnostics.DiagnosticSource & Microsoft.Extensions.DependencyInjection.Abstractions

## Problem
Application was crashing at startup with one of these errors:
1. `System.IO.FileNotFoundException: Could not load file or assembly 'System.Diagnostics.DiagnosticSource, Version=10.0.0.0'`
2. `System.IO.FileNotFoundException: Could not load file or assembly 'Microsoft.Extensions.DependencyInjection.Abstractions, Version=10.0.0.0'`

## Root Cause
- `Microsoft.Extensions.Hosting` version 8.0.1 has transitive dependencies that require version 10.0.1 of:
  - `System.Diagnostics.DiagnosticSource`
  - `Microsoft.Extensions.DependencyInjection.Abstractions`
- These assemblies were not being explicitly referenced, causing runtime resolution issues
- Visual Studio had cached temp project files with older/missing versions

## Solution Applied

### 1. Added Explicit Package References
**File: `Xcelerator.csproj`**
```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.1" />
<PackageReference Include="System.Diagnostics.DiagnosticSource" Version="10.0.1" />
```

### 2. Updated Assembly Binding Redirects
**File: `Xcelerator\App.config`**
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <runtime>
    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
      <dependentAssembly>
        <assemblyIdentity name="System.Diagnostics.DiagnosticSource" 
                          publicKeyToken="cc7b13ffcd2ddd51" 
                          culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-10.0.1.0" 
                         newVersion="10.0.1.0" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="Microsoft.Extensions.DependencyInjection.Abstractions" 
                          publicKeyToken="adb9793829ddae60" 
                          culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-10.0.1.0" 
                         newVersion="10.0.1.0" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>
```

### 3. Post-Build Configuration Copy (Already in place)
**File: `Xcelerator.csproj`**
```xml
<Target Name="CopyConfigToExe" AfterTargets="Build">
  <Copy SourceFiles="$(OutputPath)\$(AssemblyName).dll.config" 
        DestinationFiles="$(OutputPath)\$(AssemblyName).exe.config" 
        Condition="Exists('$(OutputPath)\$(AssemblyName).dll.config')" />
</Target>
```

This ensures both `Xcelerator.dll.config` and `Xcelerator.exe.config` exist in the output directory.

### 4. Cleanup Steps Performed
- Removed all cached temp project files from `%LOCALAPPDATA%\Temp`
- Deleted `bin` and `obj` directories
- Performed `dotnet clean`
- Performed `dotnet restore --force`
- Full rebuild

## Verification
✅ Application starts successfully  
✅ Application responds normally  
✅ Correct DLL versions (10.0.1) present in output directory:
  - `Microsoft.Extensions.DependencyInjection.Abstractions.dll`
  - `System.Diagnostics.DiagnosticSource.dll`
✅ Assembly binding redirects in place for both assemblies  

## Important Notes
- Always close Visual Studio when making project file changes
- Clean temp files if experiencing persistent assembly loading issues: `Remove-Item "$env:LOCALAPPDATA\Temp\*.csproj" -Force`
- The App.config file is automatically copied to both .dll.config and .exe.config during build
- When using `Microsoft.Extensions.Hosting` 8.0.1, ensure all transitive dependencies are at version 10.0.1

## Package Versions Used
- `Microsoft.Extensions.Hosting`: 8.0.1
- `Microsoft.Extensions.DependencyInjection.Abstractions`: 10.0.1
- `System.Diagnostics.DiagnosticSource`: 10.0.1

## Related Files Modified
1. `Xcelerator\Xcelerator.csproj` - Added package references and post-build target
2. `Xcelerator\App.config` - Created with assembly binding redirects for both assemblies
3. `Xcelerator\App.xaml.cs` - Moved VelopackApp initialization to OnStartup method

## Date
January 2025 (Updated with DependencyInjection.Abstractions fix)
