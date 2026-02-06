# Diagnostic Guide - Published Application Issues

## NEW: Automatic Diagnostic Logging

The application now writes a detailed diagnostic log file that you can use to troubleshoot issues with the published application.

### Where is the Log File?

The log is automatically created at:
```
%APPDATA%\Xcelerator\diagnostic.log
```

Full path example:
```
C:\Users\YourName\AppData\Roaming\Xcelerator\diagnostic.log
```

### How to View the Log

**Option 1: Automatic (on error)**
When the application encounters an error loading configuration files, it will:
1. Show an error dialog
2. **Automatically open the log file in Notepad**

**Option 2: Manual**
Navigate to the log file location:
```powershell
# Open the log file
notepad "$env:APPDATA\Xcelerator\diagnostic.log"

# Or open the folder
explorer "$env:APPDATA\Xcelerator"
```

## What the Log Contains

The log shows:
- ✅ Application startup information
- ✅ Base directory and executable paths
- ✅ Every path checked for resource files
- ✅ Which files were found/not found
- ✅ File read success/failure
- ✅ Deserialization results
- ✅ Complete exception details with stack traces

### Example Log Output

```
=== Xcelerator Diagnostic Log ===
Started at: 2026-02-01 22:30:15
Base Directory: C:\MyApp\publish\
Executable Path: C:\MyApp\publish\Xcelerator.exe
Executable Directory: C:\MyApp\publish
================================
[22:30:15.123] [ResolveResourcePath] Looking for: cluster.json
[22:30:15.124] [ResolveResourcePath] Base Directory: C:\MyApp\publish\
[22:30:15.125] [ResolveResourcePath] Checking: C:\MyApp\publish\Resources\cluster.json
[22:30:15.126] [ResolveResourcePath] ✓ FOUND at: C:\MyApp\publish\Resources\cluster.json
[22:30:15.127] [InitializeClusters] Reading file: C:\MyApp\publish\Resources\cluster.json
[22:30:15.150] [InitializeClusters] File read successfully, length: 16239 chars
[22:30:15.180] [InitializeClusters] Deserialized 45 cluster configs
[22:30:15.181] [InitializeClusters] Added cluster: C1-Cluster01
[22:30:15.182] [InitializeClusters] Added cluster: C2-Cluster02
...
[22:30:15.250] [InitializeClusters] ✓ Successfully loaded 45 clusters
```

### Example Error Log

```
[22:30:15.123] [ResolveResourcePath] Looking for: cluster.json
[22:30:15.124] [ResolveResourcePath] Checking: C:\MyApp\publish\Resources\cluster.json
[22:30:15.125] [ResolveResourcePath] ✗ Not found: C:\MyApp\publish\Resources\cluster.json
[22:30:15.126] [ResolveResourcePath] Checking: C:\MyApp\publish\Resources\cluster.json
[22:30:15.127] [ResolveResourcePath] ✗ Not found: C:\MyApp\publish\Resources\cluster.json
[22:30:15.128] [ResolveResourcePath] Checking: C:\MyApp\publish\cluster.json
[22:30:15.129] [ResolveResourcePath] ✗ Not found: C:\MyApp\publish\cluster.json
[22:30:15.130] [ResolveResourcePath] Checking: C:\XceleratorTool\Resources\cluster.json
[22:30:15.131] [ResolveResourcePath] ✗ Not found: C:\XceleratorTool\Resources\cluster.json
[22:30:15.132] [ResolveResourcePath] ❌ NOT FOUND: cluster.json
[22:30:15.133] [InitializeClusters] ❌ ERROR: cluster.json not found!
```

## Troubleshooting Steps

### Step 1: Run the Published Application

```powershell
# Publish the application
dotnet publish -c Release -r win-x64 --self-contained -o ./publish

# Run it
./publish/Xcelerator.exe
```

### Step 2: Check for Error Dialog

If you see an error dialog, it will tell you the log file location.

### Step 3: Review the Diagnostic Log

The log will show you:
1. **Where the app looked** for files
2. **What it found** (or didn't find)
3. **Any errors** that occurred

### Step 4: Verify Files Exist

Based on the log, check if the files exist where they should:

```powershell
# Check if files were copied to publish directory
dir ./publish/Resources/

# Check if fallback location exists
dir C:\XceleratorTool\Resources\
```

### Step 5: Common Issues and Solutions

#### Issue 1: Files Not Copied to Publish Directory

**Symptom in log:**
```
[ResolveResourcePath] ✗ Not found: C:\MyApp\publish\Resources\cluster.json
```

**Solution:**
Verify MSBuild target ran. Re-publish:
```powershell
dotnet publish -c Release -r win-x64 --self-contained -o ./publish
```

Check build output for:
```
Copied external resource files to .\publish\Resources\
```

#### Issue 2: Base Directory is Wrong

**Symptom in log:**
```
Base Directory: C:\Windows\System32\
```

**Solution:**
This happens if the app is run with wrong permissions or via certain shortcuts. Run directly:
```powershell
cd <publish-directory>
.\Xcelerator.exe
```

#### Issue 3: Access Denied

**Symptom in log:**
```
[InitializeClusters] ❌ EXCEPTION: UnauthorizedAccessException: Access to the path is denied.
```

**Solution:**
Run the application from a location where you have read permissions, or run as administrator.

#### Issue 4: Corrupted JSON File

**Symptom in log:**
```
[InitializeClusters] File read successfully, length: 16239 chars
[InitializeClusters] ❌ EXCEPTION: JsonException: ...
```

**Solution:**
The JSON file is invalid. Validate it:
```powershell
Get-Content ./publish/Resources/cluster.json | ConvertFrom-Json
```

## Quick Diagnostic Command

Run this PowerShell script to gather all diagnostic information:

```powershell
Write-Host "=== Xcelerator Diagnostic Check ===" -ForegroundColor Cyan
Write-Host ""

$publishDir = "./publish"
$appDataLog = "$env:APPDATA\Xcelerator\diagnostic.log"

Write-Host "1. Checking publish directory..." -ForegroundColor Yellow
if (Test-Path $publishDir) {
    Write-Host "   ✓ Publish directory exists: $publishDir" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "2. Checking Resources folder..." -ForegroundColor Yellow
    if (Test-Path "$publishDir\Resources") {
        Write-Host "   ✓ Resources folder exists" -ForegroundColor Green
        
        Write-Host ""
        Write-Host "3. Checking resource files..." -ForegroundColor Yellow
        $files = @("cluster.json", "servers.json")
        foreach ($file in $files) {
            if (Test-Path "$publishDir\Resources\$file") {
                $size = (Get-Item "$publishDir\Resources\$file").Length
                Write-Host "   ✓ $file exists ($size bytes)" -ForegroundColor Green
            } else {
                Write-Host "   ✗ $file MISSING!" -ForegroundColor Red
            }
        }
    } else {
        Write-Host "   ✗ Resources folder MISSING!" -ForegroundColor Red
    }
} else {
    Write-Host "   ✗ Publish directory not found!" -ForegroundColor Red
}

Write-Host ""
Write-Host "4. Checking fallback location..." -ForegroundColor Yellow
if (Test-Path "C:\XceleratorTool\Resources") {
    Write-Host "   ✓ C:\XceleratorTool\Resources exists" -ForegroundColor Green
} else {
    Write-Host "   ⚠ C:\XceleratorTool\Resources not found (fallback unavailable)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "5. Checking diagnostic log..." -ForegroundColor Yellow
if (Test-Path $appDataLog) {
    Write-Host "   ✓ Diagnostic log exists" -ForegroundColor Green
    Write-Host "   Location: $appDataLog" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   Last 20 lines:" -ForegroundColor Gray
    Get-Content $appDataLog -Tail 20 | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
} else {
    Write-Host "   ⚠ No diagnostic log yet (app hasn't run)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
```

Save this as `check-diagnostic.ps1` and run it after publishing.

## Summary

✅ **Diagnostic log** is automatically created at `%APPDATA%\Xcelerator\diagnostic.log`  
✅ **Auto-opens** when errors occur  
✅ **Shows every path** checked  
✅ **Includes full exception details**  
✅ **Easy to share** for troubleshooting  

If you encounter issues, **check the diagnostic log first** - it will tell you exactly what's happening!
