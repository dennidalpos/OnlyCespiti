param(
    [switch]$IncludeDotnetClean,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$KeepPublish
)

$ErrorActionPreference = 'Stop'

function Write-Info([string]$message) { Write-Host $message -ForegroundColor Cyan }
function Write-Ok([string]$message) { Write-Host $message -ForegroundColor Green }
function Get-RelativePath([string]$path) {
    return $path.Replace($projectRoot + [IO.Path]::DirectorySeparatorChar, '')
}

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
$projectFile = Join-Path $projectRoot 'GestioneCespiti.csproj'

Set-Location $projectRoot

Write-Info 'Pulizia progetto GestioneCespiti...'

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

$ignoredRoots = @(
    (Join-Path $projectRoot '.git')
)

$foldersToClean = @('build', 'bin', 'obj', 'dist', 'out', 'target', 'artifacts', 'TestResults', '.vs', 'data', 'tmp')
if (-not $KeepPublish) {
    $foldersToClean += 'publish'
}

$foldersToDelete = Get-ChildItem -Path $projectRoot -Directory -Recurse -Force |
    Where-Object {
        $_.Name -in $foldersToClean
    } |
    Where-Object {
        $current = $_.FullName
        -not ($ignoredRoots | Where-Object { $current.StartsWith($_, [System.StringComparison]::OrdinalIgnoreCase) })
    } |
    Sort-Object { $_.FullName.Length } -Descending

foreach ($folder in $foldersToDelete) {
    if (Test-Path $folder.FullName) {
        Remove-Item -Path $folder.FullName -Recurse -Force
        Write-Ok "Rimosso: $(Get-RelativePath $folder.FullName)"
    }
}

$tempFiles = Get-ChildItem -Path $projectRoot -File -Recurse -Force |
    Where-Object {
        $_.Extension -in @('.log', '.tmp', '.bak')
    } |
    Where-Object {
        $current = $_.FullName
        -not ($ignoredRoots | Where-Object { $current.StartsWith($_, [System.StringComparison]::OrdinalIgnoreCase) })
    }

foreach ($file in $tempFiles) {
    Remove-Item -Path $file.FullName -Force
    Write-Ok "Rimosso: $(Get-RelativePath $file.FullName)"
}

if ($KeepPublish) {
    Write-Info 'Mantengo le cartelle publish su richiesta (-KeepPublish).'
}

Write-Ok 'Pulizia completata.'
