param(
    [ValidateSet('win-x64','win-x86','win-arm64')]
    [string]$Runtime = 'win-x64',

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [ValidateSet('SelfContained', 'FrameworkDependent')]
    [string]$PublishMode = 'SelfContained',
    [switch]$SelfContained,
    [switch]$FrameworkDependent,
    [switch]$SkipRestore,
    [switch]$NoClean,
    [switch]$CleanOnly,
    [switch]$NoPublish,
    [switch]$IncludeDotnetClean,
    [switch]$KeepPublishOnClean
)

$ErrorActionPreference = 'Stop'

function Write-Info([string]$message) { Write-Host $message -ForegroundColor Cyan }
function Write-Ok([string]$message) { Write-Host $message -ForegroundColor Green }

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
$projectFile = Join-Path $projectRoot 'GestioneCespiti.csproj'
$cleanScript = Join-Path $scriptPath 'clean.ps1'

Set-Location $projectRoot

if (-not (Test-Path $projectFile)) {
    throw "File progetto non trovato: $projectFile"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet non trovato nel PATH. Installa .NET SDK 8 o superiore.'
}

if ($SelfContained -and $FrameworkDependent) {
    throw 'Usa solo uno tra -SelfContained e -FrameworkDependent.'
}

if ($SelfContained) {
    $PublishMode = 'SelfContained'
}
elseif ($FrameworkDependent) {
    $PublishMode = 'FrameworkDependent'
}
elseif (-not $PSBoundParameters.ContainsKey('PublishMode')) {
    Write-Host ''
    Write-Host 'Seleziona modalità publish:' -ForegroundColor Yellow
    Write-Host '  1) SelfContained' -ForegroundColor White
    Write-Host '  2) FrameworkDependent' -ForegroundColor White

    do {
        $choice = Read-Host 'Scelta [1/2] (default 1)'
        if ([string]::IsNullOrWhiteSpace($choice)) {
            $choice = '1'
        }

        switch ($choice.Trim()) {
            '1' {
                $PublishMode = 'SelfContained'
                $validChoice = $true
            }
            '2' {
                $PublishMode = 'FrameworkDependent'
                $validChoice = $true
            }
            default {
                $validChoice = $false
                Write-Host 'Valore non valido. Inserisci 1 oppure 2.' -ForegroundColor Red
            }
        }
    } while (-not $validChoice)
}

$publishSelfContained = $PublishMode -eq 'SelfContained'
Write-Info "Modalità publish selezionata: $PublishMode"

Write-Info 'Verifica dotnet SDK...'
$dotnetVersion = dotnet --version
Write-Ok ".NET SDK: $dotnetVersion"

if (-not $NoClean -or $CleanOnly) {
    if (-not (Test-Path $cleanScript)) {
        throw "Script clean non trovato: $cleanScript"
    }

    $cleanParams = @{
        Configuration = $Configuration
    }

    if ($IncludeDotnetClean) {
        $cleanParams.IncludeDotnetClean = $true
    }

    if ($KeepPublishOnClean) {
        $cleanParams.KeepPublish = $true
    }

    & $cleanScript @cleanParams
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

Write-Info "dotnet build $Configuration..."
if ($SkipRestore) {
    dotnet build $projectFile -c $Configuration --no-restore
}
else {
    dotnet build $projectFile -c $Configuration
}
Write-Ok 'Build completata.'

if ($NoPublish) {
    Write-Ok 'Publish saltato (NoPublish).'
    exit 0
}

$publishDir = Join-Path $projectRoot "publish/$Runtime"

Write-Info 'dotnet publish...'
if ($publishSelfContained) {
    dotnet publish $projectFile -c $Configuration -r $Runtime --self-contained true `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:EnableCompressionInSingleFile=true `
        -o $publishDir
}
else {
    dotnet publish $projectFile -c $Configuration -r $Runtime --self-contained false `
        /p:PublishSingleFile=true `
        /p:EnableCompressionInSingleFile=true `
        -o $publishDir
}
Write-Ok 'Publish completato.'

$dataFolder = Join-Path $publishDir 'data'
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
Write-Host "  $publishDir" -ForegroundColor White
Write-Host ''
Get-ChildItem $publishDir -File | Sort-Object Length -Descending | Select-Object -First 10 | ForEach-Object {
    $sizeMB = [math]::Round($_.Length / 1MB, 2)
    Write-Host ("  - {0} ({1} MB)" -f $_.Name, $sizeMB) -ForegroundColor Gray
}
