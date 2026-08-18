$ErrorActionPreference = "Stop"

# ============================================================
# Configuration
# ============================================================

$ProjectFile = Join-Path $PSScriptRoot "monitor-controller.csproj"
$ProjectName = "monitor-controller"
$Runtime = "win-x64"
$Configuration = "Release"

# ============================================================
# Read version from .csproj
# ============================================================

if (-not (Test-Path $ProjectFile)) {
    throw "Project file not found: $ProjectFile"
}

[xml]$ProjectXml = Get-Content $ProjectFile

$versionNode = $ProjectXml.Project.PropertyGroup.Version |
    Where-Object { $_ -ne $null } |
    Select-Object -First 1

if (-not $versionNode) {
    throw "Version was not found in $ProjectFile"
}

$Version = $versionNode.InnerText.Trim()

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Invalid version: $Version"
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " OTMC Monitor Controller Release" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Version : $Version"
Write-Host "Runtime : $Runtime"
Write-Host ""

# ============================================================
# Git checks
# ============================================================

Write-Host "[1/7] Checking Git..." -ForegroundColor Yellow

git rev-parse --is-inside-work-tree *> $null

if ($LASTEXITCODE -ne 0) {
    throw "Not a Git repository."
}

$branch = git branch --show-current

if (-not $branch) {
    throw "Unable to determine current Git branch."
}

Write-Host "Branch: $branch"

$status = git status --porcelain

if ($status) {
    Write-Host ""
    Write-Host "Warning: working tree has uncommitted changes:" -ForegroundColor Yellow
    Write-Host $status
    Write-Host ""

    $answer = Read-Host "Continue anyway? (y/N)"

    if ($answer -ne "y") {
        throw "Release cancelled."
    }
}

# ============================================================
# Check GitHub CLI
# ============================================================

Write-Host "[2/7] Checking GitHub CLI..." -ForegroundColor Yellow

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI (gh) is not installed. Install it first."
}

gh auth status

if ($LASTEXITCODE -ne 0) {
    throw "GitHub CLI is not authenticated. Run: gh auth login"
}

# ============================================================
# Publish
# ============================================================

Write-Host "[3/7] Publishing release..." -ForegroundColor Yellow

dotnet publish `
    $ProjectFile `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

$PublishDir = Join-Path `
    $PSScriptRoot `
    "bin\$Configuration\net10.0-windows\$Runtime\publish"

$Exe = Join-Path $PublishDir "$ProjectName.exe"
$ReleaseExe = Join-Path $PublishDir "$ProjectName-$Version.exe"

if (-not (Test-Path $Exe)) {
    throw "Published executable not found: $Exe"
}

# ============================================================
# Rename executable
# ============================================================

Write-Host "[4/7] Preparing release executable..." -ForegroundColor Yellow

if (Test-Path $ReleaseExe) {
    Remove-Item $ReleaseExe -Force
}

Rename-Item $Exe "$ProjectName-$Version.exe"

if (-not (Test-Path $ReleaseExe)) {
    throw "Failed to rename release executable."
}

Write-Host "Release file:"
Write-Host $ReleaseExe -ForegroundColor Green

# ============================================================
# Git tag
# ============================================================

Write-Host "[5/7] Creating Git tag..." -ForegroundColor Yellow

$Tag = "v$Version"

$existingTag = git tag --list $Tag

if ($existingTag) {
    throw "Git tag '$Tag' already exists."
}

git tag -a $Tag -m "Release $Tag"

if ($LASTEXITCODE -ne 0) {
    throw "Failed to create Git tag."
}

git push origin $Tag

if ($LASTEXITCODE -ne 0) {
    throw "Failed to push Git tag."
}

# ============================================================
# GitHub Release
# ============================================================

Write-Host "[6/7] Creating GitHub Release..." -ForegroundColor Yellow

$ReleaseNotes = @"
# OTMC Monitor Controller $Tag

## Download

Download:

`$ProjectName-$Version.exe

## Features

- Automatic monitor brightness control
- Automatic monitor contrast control
- Time-based display profiles
- DDC/CI support
- Windows system tray application

## Runtime

- Windows x64
- .NET 10
- Self-contained single-file executable
"@

$notesFile = Join-Path $env:TEMP "$ProjectName-$Version-release.md"

Set-Content `
    -Path $notesFile `
    -Value $ReleaseNotes `
    -Encoding UTF8

gh release create $Tag `
    $ReleaseExe `
    --title "$ProjectName $Tag" `
    --notes-file $notesFile

if ($LASTEXITCODE -ne 0) {
    throw "Failed to create GitHub Release."
}

Remove-Item $notesFile -Force -ErrorAction SilentlyContinue

# ============================================================
# Done
# ============================================================

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " Release completed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Version : $Version"
Write-Host "Tag     : $Tag"
Write-Host "File    : $ReleaseExe"
Write-Host ""

gh release view $Tag