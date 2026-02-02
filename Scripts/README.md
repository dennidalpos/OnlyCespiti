# Script di Build

Questa cartella contiene gli script per compilare e pubblicare l'applicazione.

## build.ps1

Script PowerShell interattivo per compilare e pubblicare l'applicazione.

### Come Usare

**Metodo 1: Da qualsiasi directory**
```powershell
.\Scripts\build.ps1
```

**Metodo 2: Dalla cartella Scripts**
```powershell
cd Scripts
.\build.ps1
```

**Metodo 3: Doppio click**
- Naviga in Esplora File alla cartella `OnlyCespiti\Scripts`
- Doppio click su `build.ps1`

### Funzionalità

Lo script offre due modalità di compilazione:

#### 1. Self-Contained (Consigliato)
- Include il runtime .NET nell'eseguibile
- File singolo di ~70-90 MB
- Funziona su qualsiasi PC Windows senza .NET installato
- Scegli il runtime:
  - win-x64 (Windows 64-bit - consigliato)
  - win-x86 (Windows 32-bit)
  - win-arm64 (Windows ARM 64-bit)

#### 2. Framework-Dependent
- Richiede .NET 8 Runtime sul PC di destinazione
- File singolo di ~1-2 MB
- Più piccolo ma necessita runtime separato

### Output

L'eseguibile viene creato in:
```
bin\Release\net8.0-windows\[runtime]\publish\
```

Esempio:
```
bin\Release\net8.0-windows\win-x64\publish\GestioneCespiti.exe
```

### Requisiti

- Windows OS
- .NET 8 SDK installato
- PowerShell 5.1 o superiore

### Risoluzione Problemi

**Errore: "Impossibile caricare il file... esecuzione degli script è disabilitata"**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

**Errore: ".NET SDK non trovato"**
- Scarica e installa .NET 8 SDK da: https://dotnet.microsoft.com/download/dotnet/8.0

**Errore: "File di progetto non trovato"**
- Assicurati che il file `GestioneCespiti.csproj` esista nella directory root
- Verifica di essere nella directory corretta del progetto

### Cosa Fa lo Script

1. ✅ Naviga automaticamente alla directory root del progetto
2. ✅ Verifica l'esistenza del file `.csproj`
3. ✅ Controlla che .NET SDK sia installato
4. ✅ Pulisce build precedenti (cartelle bin/obj)
5. ✅ Esegue `dotnet restore` per scaricare i pacchetti NuGet
6. ✅ Esegue `dotnet build` in modalità Release
7. ✅ Esegue `dotnet publish` con le opzioni scelte
8. ✅ Crea le cartelle `data/`, `config/`, `archived/`
9. ✅ Mostra dimensione e posizione dell'eseguibile finale

### Opzioni di Compilazione

Lo script usa le seguenti opzioni di `dotnet publish`:

**Self-Contained:**
```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:EnableCompressionInSingleFile=true
```

**Framework-Dependent:**
```powershell
dotnet publish -c Release -r win-x64 --self-contained false `
  /p:PublishSingleFile=true
```

### Note

- Lo script è interattivo e chiede conferme
- Al termine mostra il percorso completo dell'eseguibile
- Le build precedenti vengono automaticamente rimosse
- Viene creata la struttura cartelle necessaria per l'applicazione
