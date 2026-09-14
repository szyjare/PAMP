<#
.SYNOPSIS
    Builds and packages PAMP v1.2.0 release (Portable ZIP + Inno Setup Installer).
#>
param(
    [switch]$SelfContained = $false
)

$ErrorActionPreference = "Stop"
$rootDir = $PSScriptRoot
$version = "1.2.0"
$distDir = Join-Path $rootDir "dist"
$publishDir = Join-Path $rootDir "publish"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "  PAMP v$version - Release Builder       " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# 1. Clean previous build artifacts
if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
if (!(Test-Path $distDir)) { New-Item -ItemType Directory -Path $distDir | Out-Null }

# 2. Publish .NET 10 project
$mode = if ($SelfContained) { "Self-Contained (bundles .NET 10 runtime)" } else { "Framework-Dependent (lightweight)" }
Write-Host "`n[1/3] Publishing PAMP ($mode)..." -ForegroundColor Yellow

$publishArgs = @(
    "publish",
    (Join-Path $rootDir "PAMP\PAMP.csproj"),
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", $(if ($SelfContained) { "true" } else { "false" }),
    "-o", $publishDir
)

if ($SelfContained) {
    $publishArgs += "-p:PublishSingleFile=true"
}

dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed."
    exit 1
}

# 3. Create Portable ZIP
Write-Host "`n[2/3] Creating Portable ZIP..." -ForegroundColor Yellow
$zipSuffix = if ($SelfContained) { "standalone" } else { "portable" }
$portableZip = Join-Path $distDir "PAMP-v$version-win-x64-$zipSuffix.zip"
if (Test-Path $portableZip) { Remove-Item -Force $portableZip }
Compress-Archive -Path "$publishDir\*" -DestinationPath $portableZip -CompressionLevel Optimal
Write-Host "  -> Created: $portableZip" -ForegroundColor Green

# 4. Check for Inno Setup compiler
Write-Host "`n[3/3] Checking for Inno Setup Compiler (ISCC.exe)..." -ForegroundColor Yellow
$isccPaths = @(
    "iscc.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)

$iscc = $null
foreach ($path in $isccPaths) {
    if (Get-Command $path -ErrorAction SilentlyContinue) {
        $iscc = $path
        break
    }
    if (Test-Path $path) {
        $iscc = $path
        break
    }
}

if ($iscc) {
    Write-Host "  Found Inno Setup at: $iscc" -ForegroundColor Green
    $issScript = Join-Path $rootDir "installer\pamp_setup.iss"
    & $iscc $issScript
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  -> Installer generated in: $distDir" -ForegroundColor Green
    } else {
        Write-Warning "Inno Setup compilation failed."
    }
} else {
    Write-Host "  Inno Setup (ISCC.exe) not found on this system." -ForegroundColor Yellow
    Write-Host "  To build the single-file Windows installer (.exe):" -ForegroundColor Cyan
    Write-Host "    1. Run: winget install JRSoftware.InnoSetup" -ForegroundColor White
    Write-Host "    2. Re-run: .\build_release.ps1" -ForegroundColor White
}

Write-Host "`nRelease build finished! Files are located in '$distDir'." -ForegroundColor Green
