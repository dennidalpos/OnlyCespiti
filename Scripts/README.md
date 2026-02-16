# Script di build e pulizia

La cartella `Scripts/` contiene due script PowerShell:

- `build.ps1`: restore, build e publish dell'applicazione.
- `clean.ps1`: rimozione artefatti locali (`bin/`, `obj/`, `publish/`).

## build.ps1

### Esempi rapidi

```powershell
# Build/publish standard (Self-Contained win-x64)
./Scripts/build.ps1

# Framework-dependent
./Scripts/build.ps1 -FrameworkDependent

# Runtime diverso
./Scripts/build.ps1 -Runtime win-arm64

# Salta restore
./Scripts/build.ps1 -SkipRestore

# Solo pulizia
./Scripts/build.ps1 -CleanOnly
```

### Parametri disponibili

- `-Runtime`: `win-x64` (default), `win-x86`, `win-arm64`
- `-SelfContained`: publish self-contained (default)
- `-FrameworkDependent`: publish framework-dependent
- `-SkipRestore`: evita `dotnet restore`
- `-NoClean`: non esegue clean iniziale
- `-CleanOnly`: esegue solo pulizia e termina

### Cosa fa

1. Verifica presenza file progetto `GestioneCespiti.csproj`
2. Verifica .NET SDK (`dotnet --version`)
3. Esegue clean (se non disattivato)
4. Esegue restore (se non disattivato)
5. Esegue build Release
6. Esegue publish con output in `publish/<runtime>/`
7. Crea la struttura dati runtime:
   - `data/`
   - `data/config/`
   - `data/config/sheets/`
   - `data/archived/`

## clean.ps1

```powershell
./Scripts/clean.ps1
```

Rimuove:

- `bin/`
- `obj/`
- `publish/`

## Requisiti

- Windows + PowerShell
- .NET SDK 8+
