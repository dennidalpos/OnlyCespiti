$ErrorActionPreference = 'Stop'

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
Set-Location $projectRoot

Write-Host 'Pulizia progetto GestioneCespiti...' -ForegroundColor Cyan

foreach ($path in @('bin','obj','publish')) {
    if (Test-Path $path) {
        Remove-Item -Path $path -Recurse -Force
        Write-Host "Rimosso: $path" -ForegroundColor Green
    }
}

Write-Host 'Pulizia completata.' -ForegroundColor Green
