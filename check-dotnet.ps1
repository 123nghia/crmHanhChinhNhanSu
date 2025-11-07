# Script kiem tra va tim duong dan dotnet
Write-Host "Dang kiem tra .NET SDK..." -ForegroundColor Yellow

# Tim dotnet trong PATH
try {
    $dotnetPath = Get-Command dotnet -ErrorAction Stop
    Write-Host "Tim thay dotnet trong PATH:" -ForegroundColor Green
    Write-Host "  $($dotnetPath.Source)" -ForegroundColor Cyan
    $version = dotnet --version
    Write-Host "  Version: $version" -ForegroundColor Cyan
} catch {
    Write-Host "Khong tim thay dotnet trong PATH" -ForegroundColor Red
    Write-Host ""
    Write-Host "Dang tim dotnet o cac vi tri thong thuong..." -ForegroundColor Yellow
    
    $commonPaths = @(
        "C:\Program Files\dotnet\dotnet.exe",
        "C:\Program Files (x86)\dotnet\dotnet.exe"
    )
    
    $found = $false
    foreach ($path in $commonPaths) {
        if (Test-Path $path) {
            Write-Host "Tim thay dotnet tai: $path" -ForegroundColor Green
            $version = & $path --version
            Write-Host "  Version: $version" -ForegroundColor Cyan
            $found = $true
            break
        }
    }
    
    if (-not $found) {
        Write-Host "Khong tim thay dotnet!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Vui long:" -ForegroundColor Yellow
        Write-Host "1. Cai dat .NET 7 SDK tu: https://dotnet.microsoft.com/download/dotnet/7.0" -ForegroundColor White
        Write-Host "2. Hoac them dotnet vao PATH environment variable" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "Kiem tra PATH hien tai:" -ForegroundColor Yellow
$pathEntries = $env:Path -split ';'
foreach ($entry in $pathEntries) {
    if ($entry -like '*dotnet*') {
        Write-Host "  $entry" -ForegroundColor Cyan
    }
}

