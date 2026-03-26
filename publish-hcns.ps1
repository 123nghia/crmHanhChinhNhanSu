param(
    [string]$ProjectPath = (Join-Path $PSScriptRoot 'crmHuman\crmHuman.csproj'),
    [string]$Configuration = 'Release',
    [string]$OutputPath = 'C:\hcns',
    [string]$MigrationsPath = (Join-Path $PSScriptRoot 'migrations')
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'Khong tim thay dotnet CLI trong PATH.'
}

if (-not (Test-Path -LiteralPath $ProjectPath)) {
    throw "Khong tim thay project can publish: $ProjectPath"
}

if (-not (Test-Path -LiteralPath $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

$resolvedProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$resolvedOutputPath = (Resolve-Path -LiteralPath $OutputPath).Path

Write-Host "Publishing $resolvedProjectPath"
Write-Host "Output: $resolvedOutputPath"
Write-Host 'Che do publish nay chi ghi de file trung ten, khong xoa cac file cu trong thu muc output.'

$publishArgs = @(
    'publish'
    $resolvedProjectPath
    '-c'
    $Configuration
    '-o'
    $resolvedOutputPath
)

& dotnet @publishArgs

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish that bai voi ma loi $LASTEXITCODE"
}

if (Test-Path -LiteralPath $MigrationsPath) {
    $resolvedMigrationsPath = (Resolve-Path -LiteralPath $MigrationsPath).Path
    $targetMigrationsPath = Join-Path $resolvedOutputPath 'migrations'
    if (-not (Test-Path -LiteralPath $targetMigrationsPath)) {
        New-Item -ItemType Directory -Path $targetMigrationsPath -Force | Out-Null
    }

    Copy-Item -Path (Join-Path $resolvedMigrationsPath '*') -Destination $targetMigrationsPath -Recurse -Force
    Write-Host "Copied migrations to $targetMigrationsPath"
}
else {
    Write-Host "Bo qua copy migrations vi khong tim thay thu muc: $MigrationsPath"
}

Write-Host 'Publish thanh cong.'
