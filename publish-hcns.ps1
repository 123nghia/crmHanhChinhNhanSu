param(
    [string]$ProjectPath = (Join-Path $PSScriptRoot 'crmHuman\crmHuman.csproj'),
    [string]$Configuration = 'Release',
    [string]$OutputPath = 'C:\hcns'
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

Write-Host 'Publish thanh cong.'
