# GestioneCespiti - Dismissioni

Applicazione desktop Windows (WinForms) per la gestione dei cespiti e delle dismissioni, con supporto per archiviazione fogli, esportazione in Excel e gestione di opzioni configurabili per alcune colonne. Il progetto è basato su .NET 8 e salva i dati localmente in formato JSON.

## Funzionalità principali

- **Gestione fogli**: crea, rinomina, salva, elimina e archivia fogli di dismissione.
- **Ricerca globale**: cerca su tutti i fogli, inclusi quelli archiviati.
- **Esportazione Excel**: esporta un foglio in formato `.xlsx`.
- **Opzioni configurabili**: gestione di opzioni per colonne come "Causa dismissione" e "Tipo asset".
- **Modalità sola lettura**: blocco applicazione per evitare modifiche simultanee su più istanze.

## Requisiti

- **Windows** (l'app è una WinForms `net8.0-windows`).
- **.NET SDK 8** per compilare dal sorgente.

## Struttura dati e cartelle

Alla prima esecuzione l'app crea una cartella `data` nella stessa directory dell'eseguibile:

```
data/
  application.log
  config/
    settings.json
    sheets/
      <nome_foglio>.settings.json
    lock.json
  archived/
  *.json
```

- `data/*.json`: fogli attivi.
- `data/archived/*.json`: fogli archiviati.
- `data/config/settings.json`: impostazioni globali.
- `data/config/sheets/*.settings.json`: impostazioni specifiche del foglio.
- `data/config/lock.json`: lock per evitare accessi concorrenti.
- `data/application.log`: log applicazione con rotazione automatica.

## Compilazione

Da **PowerShell** o **Prompt dei comandi**:

```
dotnet build
```

Per generare un eseguibile pubblicato:

```
dotnet publish -c Release -r win-x64 --self-contained false
```

> Nota: essendo un progetto WinForms, è necessario eseguire su Windows.

## Esecuzione

Avvia l'app dalla cartella di output (es. `bin/Release/net8.0-windows/`):

```
dotnet GestioneCespiti.dll
```

oppure l'eseguibile generato in fase di publish.

## Gestione fogli

Ogni foglio contiene le colonne standard iniziali:

- Tipo asset
- Marca
- Modello
- Seriale
- Rif inventario
- Descrizione
- Causa dismissione

È possibile aggiungere colonne personalizzate, eliminare righe e rinominare il foglio.

## Opzioni configurabili

Le colonne **Causa dismissione** e **Tipo asset** utilizzano liste di opzioni configurabili:

- Opzioni globali in `data/config/settings.json`.
- Opzioni specifiche per foglio in `data/config/sheets/*.settings.json`.

Le opzioni vengono sanificate (rimozione duplicati/spazi).

## Backup e resilienza dati

- Salvataggi atomici con file temporanei e backup (`.bak`) per fogli e impostazioni.
- Normalizzazione dati in caricamento (righe nulle e colonne duplicate).

## Log e diagnosi

I log vengono scritti in `data/application.log` con rotazione a 10MB. In caso di errori critici, l'app mostra messaggi a schermo e registra l'eccezione.

## Script utili

Nella cartella `Scripts/` è presente uno script PowerShell di build/publish (`build.ps1`). È utile per automatizzare restore, build e publish.

## Licenza

Non è specificata una licenza nel repository. Se desideri aggiungerla, crea un file `LICENSE` con il testo appropriato.
