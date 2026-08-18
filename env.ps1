#
# OTMC License.
# Copyright (c) 2026 OTMC Softwares. All rights reserved.
# Contributors: Trung Ng, OTMC Authors.
#

param(
    [Alias('u')]
    [switch]$Update
)

$global:TOP = Get-Location

$Target = Join-Path $HOME ".otmc\scripts"

if (Test-Path (Join-Path $Target ".git")) {
    if ($Update) {
        Write-Host "### 📚 Updating scripts ..." -ForegroundColor DarkBlue
        git -C $Target pull
        if ($LASTEXITCODE -ne 0) {
            Write-Host ">>> ❌ Failed to update scripts!" -ForegroundColor Red
            exit 1
        }
    }
} else {
    Write-Host "### 📁 Cloning scripts ..." -ForegroundColor DarkGreen
    New-Item -ItemType Directory -Force -Path (Split-Path $Target) | Out-Null
    git clone https://github.com/otmc-sw/scripts.git $Target
    if ($LASTEXITCODE -ne 0) {
        Write-Host ">>> ❌ Failed to clone scripts!" -ForegroundColor Red
        exit 1
    }
}

function e   { Set-Location $TOP; & "$TOP\env.ps1" @args }
function s   { Set-Location $TOP; & "$Target\project\setup.ps1" @args }
function t   { Set-Location $TOP; & "$Target\project\test.ps1" @args }
function p   { Set-Location $TOP; & "$Target\project\push.ps1" @args }
function f   { Set-Location $TOP; & "$Target\project\format.ps1" @args }
function b   { Set-Location $TOP; & "$Target\project\build.ps1" @args }
function tag { Set-Location $TOP; & "$Target\project\tag.ps1" @args }
function u   { Set-Location $TOP; & "$Target\project\upgrade.ps1" @args }

function run { Set-Location $TOP; & "$TOP\Scripts\run.ps1" @args }
function rel { Set-Location $TOP; & "$TOP\Scripts\release.ps1" @args }

Write-Host ""
Write-Host ">>> Environment Loaded on Windows!" -ForegroundColor DarkGreen
Write-Host ">>> Source directory:  $TOP" -ForegroundColor DarkGreen
Write-Host ">>> Tool directory:    $Target" -ForegroundColor DarkGreen
Write-Host ""

