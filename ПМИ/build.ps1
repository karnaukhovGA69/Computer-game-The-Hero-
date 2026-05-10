$ErrorActionPreference = "Stop"

$typstCommand = Get-Command typst -ErrorAction SilentlyContinue
if ($typstCommand) {
    $typst = $typstCommand.Source
} else {
    $typst = Join-Path $env:LOCALAPPDATA "Microsoft\WinGet\Packages\Typst.Typst_Microsoft.Winget.Source_8wekyb3d8bbwe\typst-x86_64-pc-windows-msvc\typst.exe"
    if (-not (Test-Path $typst)) {
        throw "Typst not found. Install Typst or add it to PATH."
    }
}

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$docName = Split-Path $PSScriptRoot -Leaf
$main = Join-Path $PSScriptRoot "src\main.typ"
$output = Join-Path $PSScriptRoot "$docName.pdf"

& $typst compile --root $projectRoot $main $output
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Built $output"
