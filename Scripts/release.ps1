
$ErrorActionPreference = "Stop"

Set-Location $PSScriptRoot/..

[xml]$csproj = Get-Content "monitor-controller.csproj"
$version = $csproj.Project.PropertyGroup.Version
$exe = "bin\monitor-controller-$version.exe"

dotnet publish -c Release

$publishDir = "bin\Release\net10.0-windows\win-x64\publish"

if (Test-Path $exe) {
    Remove-Item $exe
}

# Override existing file
Move-Item `
    "$publishDir\monitor-controller.exe" `
    $exe `
    -Force

gh release create "v$version" `
    $exe `
    --title "v$version" `
    --generate-notes
