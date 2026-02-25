# GestioneCespiti

Applicazione desktop Windows Forms per gestire fogli di dismissione cespiti, con salvataggio JSON locale, ricerca trasversale tra fogli, archiviazione e export in Excel.

## Obiettivo del progetto

Il progetto aiuta a:
- creare e mantenere più fogli operativi;
- tracciare i beni tramite colonne standard e colonne personalizzate;
- gestire opzioni controllate per campi specifici (`Causa dismissione`, `Tipo asset`);
- importare/esportare fogli in JSON in modo sicuro;
- salvare automaticamente le modifiche;
- cercare rapidamente valori in tutti i fogli, inclusi quelli archiviati;
- esportare i dati in formato `.xlsx`.

## Requisiti

- Windows 10/11
- .NET SDK 8.0 (per compilazione locale)
- PowerShell 7+ consigliata per gli script di build/clean

## Struttura del repository

- `Forms/` UI principale e dialog.
- `Managers/` logica di griglia, ricerca, stato.
- `Services/` persistenza dati, lock applicativo, export Excel, impostazioni e logging.
- `Models/` modelli dominio (`Asset`, `AssetSheet`, `AppSettings`, ecc.).
- `Scripts/` automazione build e clean.

## Avvio in sviluppo

Dalla root del repository:

```powershell
dotnet restore
dotnet build -c Release
```

Per avviare l'app in debug:

```powershell
dotnet run
```

## Script disponibili

### Build completo

```powershell
./Scripts/build.ps1
```

Comportamento predefinito:
- pulizia cartelle (`bin`, `obj`, `publish`) anche in sottocartelle;
- restore pacchetti;
- build `Release`;
- publish `win-x64` self-contained single file;
- creazione cartelle dati necessarie nel publish.

Parametri principali:
- `-Runtime win-x64|win-x86|win-arm64`
- `-Configuration Debug|Release`
- all'avvio, se non specifichi opzioni, lo script chiede in console la modalità publish (SelfContained / FrameworkDependent)
- `-PublishMode SelfContained|FrameworkDependent` (scelta consigliata da console)
  - Nota: la compressione single-file è applicata solo in modalità `SelfContained` (vincolo .NET SDK).
- `-SelfContained` o `-FrameworkDependent` (compatibilità)
- `-SkipRestore`
- `-NoClean`
- `-CleanOnly`
- `-NoPublish`
- `-IncludeDotnetClean`
- `-KeepPublishOnClean`

Esempi:
- `./Scripts/build.ps1 -PublishMode SelfContained`
- `./Scripts/build.ps1 -PublishMode FrameworkDependent`

### Clean

```powershell
./Scripts/clean.ps1
```

Rimuove le cartelle di output locali (`bin`, `obj`) in tutta la repo e, salvo override, anche `publish`.

Opzioni:
- `-IncludeDotnetClean` per eseguire anche `dotnet clean` sul `.csproj`.
- `-Configuration Debug|Release` (usata solo con `-IncludeDotnetClean`).
- `-KeepPublish` per mantenere intatta la cartella `publish`.

## Dati applicativi

A runtime l'app usa una cartella `data` accanto all'eseguibile:

- `data/*.json` fogli attivi;
- `data/archived/*.json` fogli archiviati;
- `data/config/settings.json` impostazioni globali;
- `data/config/sheets/*.json` impostazioni per foglio.

### Affidabilità salvataggio

- Salvataggio foglio su file temporaneo e sostituzione atomica del file finale.
- Creazione backup `.bak` per sovrascritture sia nei salvataggi JSON sia negli export Excel.
- Recupero automatico da backup in lettura quando un JSON principale è corrotto.

## Funzionalità principali

### Gestione fogli

- Creazione nuovo foglio con intestazione personalizzata.
- Rinomina foglio.
- Eliminazione foglio.
- Archiviazione foglio corrente.
- Ripristino o eliminazione definitiva fogli archiviati.

### Gestione righe/colonne

- Aggiunta e rimozione righe.
- Aggiunta colonne custom.
- Rimozione colonne custom (non consentita sulle colonne standard).
- Evidenziazione visiva differenziata tra colonne standard e personalizzate.

### Ricerca e filtri

Ricerca globale con due checkbox toggle (`ToolStripButton`):
- **Includi archiviati**: include i fogli in archivio nel set di ricerca.
- **Match case**: passa da confronto case-insensitive a case-sensitive.

Comportamento:
- pressione `Invio` nella casella ricerca = esegue ricerca o passa al risultato successivo;
- modifica di un filtro con testo presente = rilancio ricerca immediato;
- modifica filtro con testo vuoto = reset completo di stato, risultati e highlight;
- svuotamento manuale della casella di ricerca = reset automatico dei risultati precedenti;
- navigazione risultato con focus automatico su tab, riga e colonna.

### Import / Export

- Export del foglio corrente in Excel (`.xlsx`).
- Export del foglio corrente in JSON (`.json`).
- Import di un foglio da JSON con normalizzazione colonne/righe e nome file interno univoco.
- Nome file iniziale sanitizzato da caratteri non validi.
- Scrittura su file temporaneo, backup del file esistente e sostituzione atomica del file finale.

## Modalità di concorrenza (lock)

L'app crea un lock applicativo per evitare modifiche concorrenti da più istanze.

Se non acquisisce il lock:
- l'app si apre in **sola lettura**;
- i comandi mutanti vengono disabilitati;
- lo stato è indicato sia nel titolo finestra sia nella status bar.

## Salvataggio automatico

Le modifiche alle celle:
- aggiornano subito il modello in memoria;
- entrano in una coda di autosalvataggio;
- vengono persistite con debounce (2 secondi) per ridurre I/O;
- in caso errore rimangono in coda per tentativi successivi.

## Logging

Il servizio di logging registra:
- eventi applicativi principali;
- errori gestiti/non gestiti;
- warning su input/file incoerenti;
- esiti di import/export/salvataggio.

## Note operative

- L’app è pensata per uso locale o cartelle condivise in rete con lock applicativo.
- In sola lettura è comunque possibile consultare i fogli e utilizzare la ricerca.
