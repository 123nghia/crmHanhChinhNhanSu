# Script setup và chạy debug cho crmHuman trên Windows
# Chạy script này trong PowerShell với quyền Administrator (nếu cần)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Setup Debug cho crmHuman Project" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Kiểm tra .NET SDK
Write-Host "[1/5] Kiểm tra .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK đã cài đặt: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ .NET SDK chưa được cài đặt!" -ForegroundColor Red
    Write-Host "  Vui lòng cài đặt .NET 7 SDK từ: https://dotnet.microsoft.com/download/dotnet/7.0" -ForegroundColor Red
    exit 1
}

# Kiểm tra SSL Certificate
Write-Host ""
Write-Host "[2/5] Kiểm tra SSL Certificate..." -ForegroundColor Yellow
try {
    dotnet dev-certs https --check
    Write-Host "✓ SSL Certificate đã được cấu hình" -ForegroundColor Green
} catch {
    Write-Host "⚠ SSL Certificate chưa được trust" -ForegroundColor Yellow
    Write-Host "  Đang cài đặt SSL Certificate..." -ForegroundColor Yellow
    dotnet dev-certs https --trust
    Write-Host "✓ SSL Certificate đã được trust" -ForegroundColor Green
}

# Restore packages
Write-Host ""
Write-Host "[3/5] Restore NuGet packages..." -ForegroundColor Yellow
Set-Location "crmHuman"
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Restore packages thất bại!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Packages đã được restore" -ForegroundColor Green

# Build project
Write-Host ""
Write-Host "[4/5] Build project..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build thất bại!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Build thành công" -ForegroundColor Green

# Kiểm tra Connection String
Write-Host ""
Write-Host "[5/5] Kiểm tra Connection String..." -ForegroundColor Yellow
$appsettingsPath = "appsettings.Development.json"
if (Test-Path $appsettingsPath) {
    $appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
    if ($appsettings.ConnectionStrings.stringConnect) {
        Write-Host "✓ Connection String đã được cấu hình" -ForegroundColor Green
        Write-Host "  Server: $($appsettings.ConnectionStrings.stringConnect -replace 'Server=([^,]+).*', '$1')" -ForegroundColor Gray
    } else {
        Write-Host "⚠ Connection String chưa được cấu hình trong appsettings.Development.json" -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠ File appsettings.Development.json không tồn tại" -ForegroundColor Yellow
}

# Hoàn tất
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Setup hoàn tất!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Các cách để chạy debug:" -ForegroundColor Yellow
Write-Host "  1. Visual Studio: Mở crmHuman.sln và nhấn F5" -ForegroundColor White
Write-Host "  2. VS Code: Nhấn F5 hoặc chọn '.NET Core Launch (crmHuman)'" -ForegroundColor White
Write-Host "  3. Command Line: dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "URL: https://localhost:7164" -ForegroundColor Cyan
Write-Host ""

Set-Location ..

