# GestioneCespiti - Dismissioni

Applicazione desktop WinForms (`net8.0-windows`) per la gestione dei cespiti da dismettere, con salvataggio locale in JSON, gestione fogli archiviati, ricerca globale e export Excel.

## Obiettivo applicativo

L’applicazione è pensata per gestire più “fogli” di lavoro indipendenti (es. per mese o per reparto), ciascuno con:

- intestazione del foglio,
- set di colonne standard + colonne custom,
- righe di cespiti,
- opzioni contestuali per i campi a scelta.

## Architettura logica

### Livello UI

- `Forms/MainForm.cs`: orchestrazione principale (tab fogli, comandi menu, ricerca, stato, lock, autosalvataggio).
- `Forms/Dialogs/*.cs`: dialog secondari (archivio, opzioni, input, about).

### Livello gestione dati e logica

- `Services/DataPersistenceService.cs`: carica/salva fogli JSON, archivia/ripristina/elimina.
- `Services/SettingsService.cs`: gestione impostazioni globali e per-foglio.
- `Services/ExcelExportService.cs`: export `.xlsx`.
- `Services/LockService.cs`: lock applicativo per modalità sola lettura in seconda istanza.
- `Managers/GridManager.cs`: binding DataGridView + gestione combo/editing.
- `Managers/SearchManager.cs`: ricerca multi-foglio con filtri.

## Cartelle dati runtime

Alla prima esecuzione vengono usate/CREATE queste cartelle nella directory dell’eseguibile:

```text
data/
  application.log
  config/
    settings.json
    settings.json.bak
    sheets/
      <nome_foglio>.settings.json
      <nome_foglio>.settings.json.bak
    lock.json
  archived/
  <foglio>.json
  <foglio>.json.bak
```

### Significato

- `data/*.json`: fogli attivi.
- `data/archived/*.json`: fogli archiviati.
- `*.bak`: backup di sicurezza mantenuti per recupero automatico.
- `data/config/settings.json`: impostazioni globali.
- `data/config/sheets/*.settings.json`: impostazioni per singolo foglio.

## Flussi di import/export (aggiornati)

## Import (caricamento)

### Fogli JSON

All’avvio e in ricerca globale:

1. vengono letti i file `.json` nella cartella target;
2. se un file è vuoto/corrotto, viene tentato il recupero da `.bak`;
3. il contenuto viene normalizzato:
   - righe nulle rimosse,
   - dizionari valori null inizializzati,
   - colonna legacy `Rif inv biofer` migrata in `Rif inventario`,
   - colonne duplicate rimosse senza perdita dati residui.

### Impostazioni

Per settings globali e per-foglio:

1. lettura file principale;
2. fallback su `.bak` in caso di errore;
3. applicazione default e sanitizzazione opzioni.

## Export (salvataggio)

### Salvataggio fogli

- serializzazione JSON indentata,
- scrittura su file temporaneo,
- copia backup del file precedente (`.bak`),
- replace atomico del file principale.

### Salvataggio impostazioni

Stessa strategia del salvataggio fogli (temp + backup + replace).

### Export Excel

- validazione percorso file e directory,
- generazione workbook con header formattato,
- bordi celle, freeze prima riga, autofit colonne,
- gestione errori I/O con messaggi espliciti.

## Ricerca, filtri e logica checkbox/toggle

Nella toolbar di ricerca sono presenti:

- **Includi archiviati** (toggle): include/esclude i fogli archiviati nel dataset di ricerca.
- **Match case** (toggle): abilita confronto case-sensitive.

Comportamento:

1. `Invio` nel box ricerca avvia ricerca o passa al risultato successivo.
2. Se modifichi un toggle con risultati già presenti, la ricerca viene rieseguita automaticamente con i nuovi filtri.
3. Lo stato in basso mostra i filtri attivi e il conteggio risultati.
4. Se un risultato diventa obsoleto (struttura foglio cambiata), non viene lanciata eccezione: viene mostrato warning in status bar.

## Gestione opzioni a scelta (combo)

Per colonne:

- `Causa dismissione`
- `Tipo asset`

la griglia usa combo con elenco opzioni da settings + valori già presenti nei dati.

Nel dialog opzioni:

- le modifiche sono ora transazionali (si applicano solo con `OK`),
- `Annulla` non altera più la lista originale,
- deduplica case-insensitive e trim automatico.

## Modalità sola lettura

Se è presente lock attivo da altra istanza:

- l’app viene aperta in sola lettura,
- i comandi mutanti vengono disabilitati,
- viene mostrato stato visivo esplicito.

### Analisi apertura da altri utenti

La gestione lock è stata resa più sicura per scenari multiutente:

- se il lock è sullo **stesso host**, l’app verifica il PID e può recuperare lock orfani locali;
- se il lock è su **host diverso**, il lock non viene rimosso automaticamente (evita scritture concorrenti e falsi positivi);
- se il lock non è acquisibile e il dettaglio lock non è leggibile, l’app entra comunque in sola lettura con messaggio esplicito.

## Script build e clean

In `Scripts/`:

- `build.ps1`: script parametrico per restore/build/publish.
- `clean.ps1`: pulizia artefatti (`bin`, `obj`, `publish`).

Esempi rapidi:

```powershell
./Scripts/build.ps1
./Scripts/build.ps1 -FrameworkDependent
./Scripts/build.ps1 -Runtime win-arm64
./Scripts/build.ps1 -CleanOnly
./Scripts/clean.ps1
```

## Requisiti

- Windows
- .NET SDK 8+
- PowerShell (per gli script)

## Avvio

Dalla cartella publish:

```powershell
./GestioneCespiti.exe
```

oppure (framework-dependent):

```powershell
dotnet GestioneCespiti.dll
```
