param(
    [ValidateSet('win-x64','win-x86','win-arm64')]
    [string]$Runtime = 'win-x64',

    [switch]$SelfContained,
    [switch]$FrameworkDependent,
    [switch]$SkipRestore,
    [switch]$NoClean,
    [switch]$CleanOnly
)

$ErrorActionPreference = 'Stop'

function Write-Info([string]$message) { Write-Host $message -ForegroundColor Cyan }
function Write-Ok([string]$message) { Write-Host $message -ForegroundColor Green }
function Write-Warn([string]$message) { Write-Host $message -ForegroundColor Yellow }

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
Set-Location $projectRoot

$projectFile = Join-Path $projectRoot 'GestioneCespiti.csproj'
if (-not (Test-Path $projectFile)) {
    throw "File progetto non trovato: $projectFile"
}

if ($SelfContained -and $FrameworkDependent) {
    throw 'Usa solo uno tra -SelfContained e -FrameworkDependent.'
}

$publishSelfContained = $true
if ($FrameworkDependent) { $publishSelfContained = $false }

function Invoke-Clean {
    Write-Info 'Pulizia cartelle di build...'
    $paths = @('bin', 'obj', 'publish')
    foreach ($path in $paths) {
        if (Test-Path $path) {
            Remove-Item -Path $path -Recurse -Force
            Write-Ok "Rimosso: $path"
        }
    }
}

Write-Info 'Verifica dotnet SDK...'
$dotnetVersion = dotnet --version
Write-Ok ".NET SDK: $dotnetVersion"

if (-not $NoClean -or $CleanOnly) {
    Invoke-Clean
}

if ($CleanOnly) {
    Write-Ok 'Pulizia completata (CleanOnly).'
    exit 0
}

if (-not $SkipRestore) {
    Write-Info 'dotnet restore...'
    dotnet restore $projectFile
    Write-Ok 'Restore completato.'
}

Write-Info 'dotnet build Release...'
if ($SkipRestore) {
    dotnet build $projectFile -c Release --no-restore
} else {
    dotnet build $projectFile -c Release
}
Write-Ok 'Build completata.'

$publishDir = Join-Path $projectRoot "publish/$Runtime"
$newPublishDir = $publishDir

Write-Info 'dotnet publish...'
if ($publishSelfContained) {
    dotnet publish $projectFile -c Release -r $Runtime --self-contained true `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:EnableCompressionInSingleFile=true `
        -o $newPublishDir
} else {
    dotnet publish $projectFile -c Release -r $Runtime --self-contained false `
        /p:PublishSingleFile=true `
        /p:EnableCompressionInSingleFile=true `
        -o $newPublishDir
}
Write-Ok 'Publish completato.'

$dataFolder = Join-Path $newPublishDir 'data'
$configFolder = Join-Path $dataFolder 'config'
$archivedFolder = Join-Path $dataFolder 'archived'
$sheetSettingsFolder = Join-Path $configFolder 'sheets'

foreach ($dir in @($dataFolder, $configFolder, $archivedFolder, $sheetSettingsFolder)) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}

Write-Ok 'Struttura cartelle dati pronta.'
Write-Host ''
Write-Host 'Output publish:' -ForegroundColor Yellow
Write-Host "  $newPublishDir" -ForegroundColor White
Write-Host ''
Get-ChildItem $newPublishDir -File | Sort-Object Length -Descending | Select-Object -First 10 | ForEach-Object {
    $sizeMB = [math]::Round($_.Length / 1MB, 2)
    Write-Host ("  - {0} ({1} MB)" -f $_.Name, $sizeMB) -ForegroundColor Gray
}
