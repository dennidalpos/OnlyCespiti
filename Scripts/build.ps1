# Vai alla directory root del progetto (parent della cartella Scripts)
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
Set-Location $projectRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Build GestioneCespiti" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Directory progetto: $projectRoot" -ForegroundColor Gray
Write-Host ""

# Verifica esistenza file di progetto
$projectFile = Join-Path $projectRoot "GestioneCespiti.csproj"
if (-Not (Test-Path $projectFile)) {
    Write-Host "ERRORE: File di progetto non trovato!" -ForegroundColor Red
    Write-Host "Cercato in: $projectFile" -ForegroundColor Red
    Read-Host "Premi INVIO per uscire"
    exit 1
}

Write-Host "File progetto trovato: GestioneCespiti.csproj" -ForegroundColor Green
Write-Host ""

Write-Host "Seleziona la modalità di compilazione:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  1) Self-Contained (include runtime .NET - file singolo ~70-90 MB)" -ForegroundColor White
Write-Host "     Funziona su qualsiasi PC Windows senza .NET installato" -ForegroundColor Gray
Write-Host ""
Write-Host "  2) Framework-Dependent (richiede .NET 8 Runtime - file singolo ~1-2 MB)" -ForegroundColor White
Write-Host "     Richiede .NET 8 Runtime installato sul PC di destinazione" -ForegroundColor Gray
Write-Host "     Più piccolo ma necessita runtime separato" -ForegroundColor Gray
Write-Host ""

do {
    $choice = Read-Host "Scegli opzione (1 o 2)"
} while ($choice -ne "1" -and $choice -ne "2")

$SelfContained = ($choice -eq "1")

if ($SelfContained) {
    Write-Host ""
    Write-Host "Seleziona il runtime target:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  1) win-x64 (Windows 64-bit - consigliato)" -ForegroundColor White
    Write-Host "  2) win-x86 (Windows 32-bit)" -ForegroundColor White
    Write-Host "  3) win-arm64 (Windows ARM 64-bit)" -ForegroundColor White
    Write-Host ""

    do {
        $runtimeChoice = Read-Host "Scegli runtime (1, 2 o 3)"
    } while ($runtimeChoice -ne "1" -and $runtimeChoice -ne "2" -and $runtimeChoice -ne "3")

    $Runtime = switch ($runtimeChoice) {
        "1" { "win-x64" }
        "2" { "win-x86" }
        "3" { "win-arm64" }
    }
} else {
    $Runtime = "win-x64"
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Configurazione selezionata:" -ForegroundColor Cyan
if ($SelfContained) {
    Write-Host "  Modalità: Self-Contained" -ForegroundColor Green
} else {
    Write-Host "  Modalità: Framework-Dependent" -ForegroundColor Green
}
Write-Host "  Runtime: $Runtime" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Verifica installazione .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>$null

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRORE: .NET SDK non trovato!" -ForegroundColor Red
    Write-Host "Scarica e installa .NET 8 SDK da: https://dotnet.microsoft.com/download" -ForegroundColor Red
    Read-Host "Premi INVIO per uscire"
    exit 1
}

Write-Host ".NET SDK versione: $dotnetVersion" -ForegroundColor Green
Write-Host ""

Write-Host "Pulizia build precedenti..." -ForegroundColor Yellow
if (Test-Path "bin") {
    Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path "obj") {
    Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host "Pulizia completata." -ForegroundColor Green
Write-Host ""

Write-Host "Esecuzione dotnet restore..." -ForegroundColor Yellow
dotnet restore "GestioneCespiti.csproj"
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRORE durante il restore dei pacchetti." -ForegroundColor Red
    Read-Host "Premi INVIO per uscire"
    exit 1
}
Write-Host "Restore completato." -ForegroundColor Green
Write-Host ""

Write-Host "Esecuzione dotnet build..." -ForegroundColor Yellow
dotnet build "GestioneCespiti.csproj" -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRORE durante la compilazione." -ForegroundColor Red
    Read-Host "Premi INVIO per uscire"
    exit 1
}
Write-Host "Build completata." -ForegroundColor Green
Write-Host ""

Write-Host "Esecuzione dotnet publish..." -ForegroundColor Yellow

$projectPath = Get-Location

if ($SelfContained) {
    dotnet publish "GestioneCespiti.csproj" -c Release -r $Runtime --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true
    $outputPath = Join-Path $projectPath "bin\Release\net8.0-windows\$Runtime\publish"
} else {
    dotnet publish "GestioneCespiti.csproj" -c Release -r $Runtime --self-contained false /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true
    $outputPath = Join-Path $projectPath "bin\Release\net8.0-windows\$Runtime\publish"
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRORE durante la pubblicazione." -ForegroundColor Red
    Read-Host "Premi INVIO per uscire"
    exit 1
}
Write-Host "Publish completato." -ForegroundColor Green
Write-Host ""

Write-Host "Verifica cartella di output..." -ForegroundColor Yellow
Write-Host "Path: $outputPath" -ForegroundColor Gray

if (-Not (Test-Path $outputPath)) {
    Write-Host "ERRORE: La cartella di output non esiste!" -ForegroundColor Red
    Write-Host "Ricerca cartelle bin disponibili..." -ForegroundColor Yellow
    
    $binPath = Join-Path $projectPath "bin\Release"
    if (Test-Path $binPath) {
        Write-Host ""
        Write-Host "Cartelle trovate in bin\Release:" -ForegroundColor Cyan
        Get-ChildItem $binPath -Directory -Recurse -Depth 3 | Where-Object { $_.Name -eq "publish" } | ForEach-Object {
            Write-Host "  - $($_.FullName)" -ForegroundColor Gray
            
            $foundPath = $_.FullName
            if (Test-Path (Join-Path $foundPath "GestioneCespiti.exe")) {
                Write-Host "    ^ ESEGUIBILE TROVATO QUI!" -ForegroundColor Green
                $outputPath = $foundPath
            }
        }
    }
    
    if (-Not (Test-Path $outputPath)) {
        Read-Host "Premi INVIO per uscire"
        exit 1
    }
}

Write-Host "Cartella di output trovata." -ForegroundColor Green
Write-Host ""

$dataFolder = Join-Path $outputPath "data"
$configFolder = Join-Path $dataFolder "config"
$archivedFolder = Join-Path $dataFolder "archived"

if (-Not (Test-Path $dataFolder)) {
    Write-Host "Creazione cartella 'data'..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $dataFolder -Force | Out-Null
}

if (-Not (Test-Path $configFolder)) {
    Write-Host "Creazione cartella 'config'..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $configFolder -Force | Out-Null
}

if (-Not (Test-Path $archivedFolder)) {
    Write-Host "Creazione cartella 'archived'..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $archivedFolder -Force | Out-Null
}

Write-Host "Cartelle create con successo." -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  BUILD COMPLETATA CON SUCCESSO!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Contenuto cartella di output:" -ForegroundColor Cyan
$files = Get-ChildItem $outputPath -File
foreach ($file in $files) {
    $sizeMB = [math]::Round($file.Length / 1MB, 2)
    Write-Host "  - $($file.Name) ($sizeMB MB)" -ForegroundColor Gray
}
Write-Host ""

$exePath = Join-Path $outputPath "GestioneCespiti.exe"
$dllPath = Join-Path $outputPath "GestioneCespiti.dll"

if (Test-Path $exePath) {
    $exeFile = Get-Item $exePath
    $fileSizeMB = [math]::Round($exeFile.Length / 1MB, 2)
    
    if ($SelfContained) {
        Write-Host "Dimensione eseguibile: $fileSizeMB MB (include runtime .NET)" -ForegroundColor Yellow
        Write-Host "Può essere eseguito su qualsiasi PC Windows senza .NET installato" -ForegroundColor Yellow
    } else {
        Write-Host "Dimensione eseguibile: $fileSizeMB MB" -ForegroundColor Yellow
        Write-Host "Richiede .NET 8 Runtime installato sul PC di destinazione" -ForegroundColor Yellow
        Write-Host "Download runtime: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Cyan
    }
    
    Write-Host ""
    Write-Host "Eseguibile principale:" -ForegroundColor Green
    Write-Host "  $exePath" -ForegroundColor White
    
} elseif (Test-Path $dllPath) {
    Write-Host "ATTENZIONE: Generato file DLL invece di EXE" -ForegroundColor Yellow
    Write-Host "Questo è normale per alcune configurazioni Framework-Dependent" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Per eseguire l'applicazione usa:" -ForegroundColor Cyan
    Write-Host "  dotnet GestioneCespiti.dll" -ForegroundColor White
    Write-Host ""
    Write-Host "File principale:" -ForegroundColor Yellow
    Write-Host "  $dllPath" -ForegroundColor White
    
} else {
    Write-Host "ERRORE: Nessun file eseguibile o DLL principale trovato!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Verifica che il progetto sia configurato correttamente." -ForegroundColor Yellow
}

Write-Host ""

if (-Not $SelfContained) {
    $dllCount = (Get-ChildItem "$outputPath\*.dll" -ErrorAction SilentlyContinue).Count
    if ($dllCount -gt 0) {
        Write-Host "NOTA: Trovati $dllCount file DLL nella cartella di output" -ForegroundColor Yellow
        Write-Host "Questi file sono necessari per l'esecuzione dell'applicazione" -ForegroundColor Yellow
        Write-Host "Copia l'intera cartella quando distribuisci l'applicazione" -ForegroundColor Yellow
        Write-Host ""
    }
}

Write-Host "Cartella completa di output:" -ForegroundColor Cyan
Write-Host "  $outputPath" -ForegroundColor White
Write-Host ""

if (Test-Path $exePath) {
    Write-Host "Per avviare l'applicazione:" -ForegroundColor Cyan
    Write-Host "  cd `"$outputPath`"" -ForegroundColor Gray
    Write-Host "  .\GestioneCespiti.exe" -ForegroundColor Gray
} elseif (Test-Path $dllPath) {
    Write-Host "Per avviare l'applicazione:" -ForegroundColor Cyan
    Write-Host "  cd `"$outputPath`"" -ForegroundColor Gray
    Write-Host "  dotnet GestioneCespiti.dll" -ForegroundColor Gray
}

Write-Host ""
Read-Host "Premi INVIO per uscire"
