$ErrorActionPreference = "Stop"

$documentDirs = Get-ChildItem -Directory $PSScriptRoot | Where-Object {
    (Test-Path (Join-Path $_.FullName "src\main.typ")) -and
    (Test-Path (Join-Path $_.FullName "build.ps1"))
} | Sort-Object Name

foreach ($documentDir in $documentDirs) {
    & (Join-Path $documentDir.FullName "build.ps1")
}
