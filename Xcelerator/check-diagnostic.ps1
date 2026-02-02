Write-Host "=== Xcelerator Diagnostic Check ===" -ForegroundColor Cyan
Write-Host ""

$publishDir = "./publish"
$appDataLog = "$env:APPDATA\Xcelerator\diagnostic.log"

Write-Host "1. Checking publish directory..." -ForegroundColor Yellow
if (Test-Path $publishDir) {
    Write-Host "   ✓ Publish directory exists: $(Resolve-Path $publishDir)" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "2. Checking Resources folder..." -ForegroundColor Yellow
    if (Test-Path "$publishDir\Resources") {
        Write-Host "   ✓ Resources folder exists" -ForegroundColor Green
        
        Write-Host ""
        Write-Host "3. Checking resource files..." -ForegroundColor Yellow
        $files = @("cluster.json", "servers.json", "ColorConfig.json")
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
    Write-Host "   ✗ Publish directory not found at: $publishDir" -ForegroundColor Red
}

Write-Host ""
Write-Host "4. Checking fallback location..." -ForegroundColor Yellow
if (Test-Path "C:\XceleratorTool\Resources") {
    Write-Host "   ✓ C:\XceleratorTool\Resources exists" -ForegroundColor Green
    $files = @("cluster.json", "servers.json")
    foreach ($file in $files) {
        if (Test-Path "C:\XceleratorTool\Resources\$file") {
            $size = (Get-Item "C:\XceleratorTool\Resources\$file").Length
            Write-Host "   ✓ $file exists ($size bytes)" -ForegroundColor Green
        } else {
            Write-Host "   ✗ $file MISSING!" -ForegroundColor Red
        }
    }
} else {
    Write-Host "   ⚠ C:\XceleratorTool\Resources not found (fallback unavailable)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "5. Checking diagnostic log..." -ForegroundColor Yellow
if (Test-Path $appDataLog) {
    $logSize = (Get-Item $appDataLog).Length
    $logTime = (Get-Item $appDataLog).LastWriteTime
    Write-Host "   ✓ Diagnostic log exists" -ForegroundColor Green
    Write-Host "   Location: $appDataLog" -ForegroundColor Gray
    Write-Host "   Size: $logSize bytes" -ForegroundColor Gray
    Write-Host "   Last modified: $logTime" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   Last 30 lines:" -ForegroundColor Gray
    Write-Host "   " + ("-" * 70) -ForegroundColor DarkGray
    Get-Content $appDataLog -Tail 30 | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
    Write-Host "   " + ("-" * 70) -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   To view full log, run: notepad `"$appDataLog`"" -ForegroundColor Cyan
} else {
    Write-Host "   ⚠ No diagnostic log yet (app hasn't run)" -ForegroundColor Yellow
    Write-Host "   Log will be created at: $appDataLog" -ForegroundColor Gray
}

Write-Host ""
Write-Host "6. Running published application..." -ForegroundColor Yellow
if (Test-Path "$publishDir\Xcelerator.exe") {
    Write-Host "   Starting Xcelerator.exe..." -ForegroundColor Gray
    Write-Host "   (Application will start in separate window)" -ForegroundColor Gray
    Write-Host ""
    
    Start-Process "$publishDir\Xcelerator.exe"
    
    Write-Host "   ⏳ Waiting 3 seconds for app to initialize..." -ForegroundColor Gray
    Start-Sleep -Seconds 3
    
    Write-Host ""
    Write-Host "   Checking diagnostic log again..." -ForegroundColor Yellow
    if (Test-Path $appDataLog) {
        Write-Host "   ✓ Log file updated!" -ForegroundColor Green
        Write-Host ""
        Write-Host "   Latest log entries:" -ForegroundColor Gray
        Write-Host "   " + ("-" * 70) -ForegroundColor DarkGray
        Get-Content $appDataLog -Tail 20 | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
        Write-Host "   " + ("-" * 70) -ForegroundColor DarkGray
    } else {
        Write-Host "   ⚠ Log file not created yet" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ✗ Xcelerator.exe not found in publish directory!" -ForegroundColor Red
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor White
Write-Host "  - If the app is running, check for error dialogs" -ForegroundColor Gray
Write-Host "  - Review the diagnostic log for detailed path information" -ForegroundColor Gray
Write-Host "  - Use: notepad `"$appDataLog`"" -ForegroundColor Cyan
Write-Host ""
