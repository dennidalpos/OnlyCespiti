param(
    [switch]$IncludeDotnetClean,
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

function Write-Info([string]$message) { Write-Host $message -ForegroundColor Cyan }
function Write-Ok([string]$message) { Write-Host $message -ForegroundColor Green }

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
$projectFile = Join-Path $projectRoot 'GestioneCespiti.csproj'

Set-Location $projectRoot

Write-Info 'Pulizia progetto GestioneCespiti...'

foreach ($path in @('bin', 'obj', 'publish')) {
    $fullPath = Join-Path $projectRoot $path
    if (Test-Path $fullPath) {
        Remove-Item -Path $fullPath -Recurse -Force
        Write-Ok "Rimosso: $path"
    }
}

if ($IncludeDotnetClean) {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'dotnet non trovato nel PATH. Installa .NET SDK o rilancia senza -IncludeDotnetClean.'
    }

    if (-not (Test-Path $projectFile)) {
        throw "File progetto non trovato: $projectFile"
    }

    Write-Info "dotnet clean ($Configuration)..."
    dotnet clean $projectFile -c $Configuration
    Write-Ok 'dotnet clean completato.'
}

Write-Ok 'Pulizia completata.'
