# PowerShell script to publish the application with resource files
# This script publishes the app and copies the JSON configuration files

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputPath = "./publish"
)

Write-Host "Publishing Xcelerator application..." -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Runtime: $Runtime" -ForegroundColor Gray
Write-Host "Output Path: $OutputPath" -ForegroundColor Gray
Write-Host ""

# Step 1: Run dotnet publish
Write-Host "Step 1: Running dotnet publish..." -ForegroundColor Yellow
$publishCommand = "dotnet publish -c $Configuration -r $Runtime --self-contained -o $OutputPath"
Write-Host "Command: $publishCommand" -ForegroundColor Gray

try {
    Invoke-Expression $publishCommand
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Publish failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Publish completed successfully" -ForegroundColor Green
} catch {
    Write-Host "❌ Error during publish: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 2: Create Resources folder in publish directory
Write-Host "Step 2: Creating Resources folder in publish directory..." -ForegroundColor Yellow
$resourcesPath = Join-Path $OutputPath "Resources"

if (!(Test-Path $resourcesPath)) {
    New-Item -ItemType Directory -Path $resourcesPath -Force | Out-Null
    Write-Host "✅ Resources folder created: $resourcesPath" -ForegroundColor Green
} else {
    Write-Host "✅ Resources folder already exists: $resourcesPath" -ForegroundColor Green
}

Write-Host ""

# Step 3: Copy JSON files from C:\XceleratorTool\Resources
Write-Host "Step 3: Copying configuration files..." -ForegroundColor Yellow

$sourceFiles = @(
    @{
        Source = "C:\XceleratorTool\Resources\cluster.json"
        Destination = Join-Path $resourcesPath "cluster.json"
        Name = "cluster.json"
    },
    @{
        Source = "C:\XceleratorTool\Resources\servers.json"
        Destination = Join-Path $resourcesPath "servers.json"
        Name = "servers.json"
    }
)

$copySuccess = $true

foreach ($file in $sourceFiles) {
    if (Test-Path $file.Source) {
        try {
            Copy-Item -Path $file.Source -Destination $file.Destination -Force
            Write-Host "  ✅ Copied $($file.Name)" -ForegroundColor Green
        } catch {
            Write-Host "  ❌ Failed to copy $($file.Name): $_" -ForegroundColor Red
            $copySuccess = $false
        }
    } else {
        Write-Host "  ⚠️  Warning: $($file.Name) not found at $($file.Source)" -ForegroundColor Yellow
        $copySuccess = $false
    }
}

Write-Host ""

# Step 4: Summary
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
if ($copySuccess) {
    Write-Host "✅ PUBLISH COMPLETED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Published application location:" -ForegroundColor White
    Write-Host "  $((Resolve-Path $OutputPath).Path)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Configuration files included:" -ForegroundColor White
    Write-Host "  ✅ cluster.json" -ForegroundColor Green
    Write-Host "  ✅ servers.json" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now deploy the application from: $OutputPath" -ForegroundColor White
} else {
    Write-Host "⚠️  PUBLISH COMPLETED WITH WARNINGS" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Some configuration files were not copied." -ForegroundColor Yellow
    Write-Host "The application will fall back to C:\XceleratorTool\Resources\" -ForegroundColor Yellow
    Write-Host "Make sure this directory exists on the target machine." -ForegroundColor Yellow
}
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
