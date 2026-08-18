
$ErrorActionPreference = "Stop"

Set-Location $PSScriptRoot/..

$version = "0.1.1"
$tag = "v$version"
$exe = "bin\monitor-controller-$version.exe"

dotnet publish `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true

$publishDir = "bin\Release\net10.0-windows\win-x64\publish"

Move-Item `
    "$publishDir\monitor-controller.exe" `
    $exe

git add .
git commit -m "Release $tag"
git push origin main

git tag $tag
git push origin $tag

gh release create $tag `
    $exe `
    --title "Monitor Controller $tag" `
    --generate-notes

Write-Host "Release $tag created successfully."