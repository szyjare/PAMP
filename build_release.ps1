<#
.SYNOPSIS
    Builds and packages PAMP v1.2.1 release (Portable ZIP + Inno Setup Installer).
#>
param(
    [switch]$SelfContained = $false,
    [switch]$All = $false
)

$ErrorActionPreference = "Stop"
$rootDir = $PSScriptRoot
$version = "1.2.1"
$distDir = Join-Path $rootDir "dist"
$publishDir = Join-Path $rootDir "publish"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "  PAMP v$version - Release Builder       " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# Locate Inno Setup compiler
$isccPaths = @(
    "iscc.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe")
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

if (!(Test-Path $distDir)) { New-Item -ItemType Directory -Path $distDir | Out-Null }

function Build-ReleasePackage([bool]$isSelfContained) {
    $mode = if ($isSelfContained) { "Self-Contained (bundles .NET 10 runtime)" } else { "Framework-Dependent (lightweight)" }
    $zipSuffix = if ($isSelfContained) { "standalone" } else { "portable" }
    $installerName = if ($isSelfContained) { "PAMP-Setup-$version-standalone" } else { "PAMP-Setup-$version" }

    Write-Host "`n-----------------------------------------" -ForegroundColor Cyan
    Write-Host " Building: $mode" -ForegroundColor Cyan
    Write-Host "-----------------------------------------" -ForegroundColor Cyan

    # 1. Clean previous publish folder
    if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }

    # 2. Publish .NET 10 project
    Write-Host "[1/3] Publishing PAMP ($mode)..." -ForegroundColor Yellow

    $publishArgs = @(
        "publish",
        (Join-Path $rootDir "PAMP\PAMP.csproj"),
        "-c", "Release",
        "-r", "win-x64",
        "--self-contained", $(if ($isSelfContained) { "true" } else { "false" }),
        "-o", $publishDir
    )

    if ($isSelfContained) {
        $publishArgs += "-p:PublishSingleFile=true"
    }

    dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed."
        exit 1
    }

    # 3. Create Portable ZIP
    Write-Host "`n[2/3] Creating Portable ZIP..." -ForegroundColor Yellow
    $portableZip = Join-Path $distDir "PAMP-v$version-win-x64-$zipSuffix.zip"
    if (Test-Path $portableZip) { Remove-Item -Force $portableZip }
    Compress-Archive -Path "$publishDir\*" -DestinationPath $portableZip -CompressionLevel Optimal
    Write-Host "  -> Created: $portableZip" -ForegroundColor Green

    # 4. Compile Inno Setup Installer
    Write-Host "`n[3/3] Building Inno Setup Installer..." -ForegroundColor Yellow
    if ($iscc) {
        $issScript = Join-Path $rootDir "installer\pamp_setup.iss"
        $isccArgs = @()
        if ($isSelfContained) {
            $isccArgs += "/DSelfContained=1"
        }
        $isccArgs += "/DOutputBaseFilename=$installerName"
        $isccArgs += $issScript

        & $iscc @isccArgs
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  -> Installer generated: $distDir\$installerName.exe" -ForegroundColor Green
        } else {
            Write-Warning "Inno Setup compilation failed for $installerName."
        }
    } else {
        Write-Host "  Inno Setup (ISCC.exe) not found on this system. Skipping .exe installer creation." -ForegroundColor Yellow
    }
}

if ($All) {
    Build-ReleasePackage $false
    Build-ReleasePackage $true
} else {
    Build-ReleasePackage $SelfContained
}

if (!$iscc) {
    Write-Host "`nNote: To build single-file Windows installers (.exe):" -ForegroundColor Yellow
    Write-Host "  1. Run: winget install JRSoftware.InnoSetup" -ForegroundColor White
    Write-Host "  2. Re-run: .\build_release.ps1 [-All | -SelfContained]" -ForegroundColor White
}

Write-Host "`nRelease build finished! Files are located in '$distDir'." -ForegroundColor Green
