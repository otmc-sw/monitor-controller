
$ErrorActionPreference = "Stop"

Set-Location $PSScriptRoot/..

[xml]$csproj = Get-Content "monitor-controller.csproj"
$version = $csproj.Project.PropertyGroup.Version
$exe = "bin\monitor-controller-$version.exe"

dotnet publish `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true

$publishDir = "bin\Release\net10.0-windows\win-x64\publish"

# Override existing file
Move-Item `
    "$publishDir\monitor-controller.exe" `
    $exe `
    -Force

