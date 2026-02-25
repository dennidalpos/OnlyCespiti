param(
    [switch]$IncludeDotnetClean,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$KeepPublish
)

$ErrorActionPreference = 'Stop'

function Write-Info([string]$message) { Write-Host $message -ForegroundColor Cyan }
function Write-Ok([string]$message) { Write-Host $message -ForegroundColor Green }

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
$projectFile = Join-Path $projectRoot 'GestioneCespiti.csproj'

Set-Location $projectRoot

Write-Info 'Pulizia progetto GestioneCespiti...'

$foldersToDelete = Get-ChildItem -Path $projectRoot -Directory -Recurse -Force |
    Where-Object {
        ($_.Name -in @('bin', 'obj')) -and
        $_.FullName -notlike "*$([IO.Path]::DirectorySeparatorChar).git$([IO.Path]::DirectorySeparatorChar)*"
    }

foreach ($folder in $foldersToDelete) {
    Remove-Item -Path $folder.FullName -Recurse -Force
    Write-Ok "Rimosso: $($folder.FullName.Replace($projectRoot + [IO.Path]::DirectorySeparatorChar, ''))"
}

if (-not $KeepPublish) {
    $publishPath = Join-Path $projectRoot 'publish'
    if (Test-Path $publishPath) {
        Remove-Item -Path $publishPath -Recurse -Force
        Write-Ok 'Rimosso: publish'
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
