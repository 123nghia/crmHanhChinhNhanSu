# Script chạy debug nhanh cho crmHuman
# Chạy script này để khởi động ứng dụng ở chế độ debug

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Chạy Debug crmHuman" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Set-Location "crmHuman"

# Set environment variable
$env:ASPNETCORE_ENVIRONMENT = "Development"

Write-Host "Đang khởi động ứng dụng..." -ForegroundColor Yellow
Write-Host "URL: https://localhost:7164" -ForegroundColor Cyan
Write-Host "Nhấn Ctrl+C để dừng" -ForegroundColor Gray
Write-Host ""

# Chạy với dotnet run
dotnet run

Set-Location ..

