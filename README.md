# Gestione Cespiti - Dismissioni

Applicazione Windows Forms per la gestione delle dismissioni dei cespiti aziendali.

## Struttura del Progetto

```
OnlyCespiti/
├── Forms/                          # Interfacce grafiche
│   ├── MainForm.cs                 # Form principale dell'applicazione
│   ├── MainForm.Designer.cs        # Designer del form principale
│   └── Dialogs/                    # Finestre di dialogo
│       ├── InputDialog.cs          # Dialog per input testuale
│       ├── OptionsDialog.cs        # Dialog gestione opzioni
│       └── ArchiveDialog.cs        # Dialog visualizzazione archivio
│
├── Models/                         # Modelli dati
│   ├── Asset.cs                    # Modello singolo cespite
│   ├── AssetSheet.cs               # Modello foglio di lavoro
│   ├── AppSettings.cs              # Impostazioni applicazione
│   ├── AppLock.cs                  # Modello lock applicazione
│   └── SearchResult.cs             # Risultato ricerca
│
├── Services/                       # Servizi applicativi
│   ├── DataPersistenceService.cs   # Persistenza dati JSON
│   ├── ExcelExportService.cs       # Esportazione Excel
│   ├── SettingsService.cs          # Gestione impostazioni
│   ├── LockService.cs              # Gestione lock multi-utente
│   └── Logger.cs                   # Sistema di logging
│
├── Properties/                     # Proprietà progetto
├── Scripts/                        # Script di build e utility
│   └── build.ps1                   # Script PowerShell per build
│
├── Program.cs                      # Entry point applicazione
├── GestioneCespiti.csproj          # File progetto
├── .gitignore                      # Git ignore
└── README.md                       # Questo file
```

## Funzionalità Principali

### Gestione Fogli
- Creazione nuovi fogli di dismissione
- Rinomina fogli esistenti
- Archiviazione e ripristino fogli
- Eliminazione fogli

### Gestione Dati
- **Numerazione Righe**: Le righe sono numerate automaticamente nella colonna "#"
- Aggiunta/rimozione righe (cespiti)
- Aggiunta/rimozione colonne personalizzate
- Modifica valori con salvataggio automatico
- **Ricerca Avanzata**: Ricerca globale in tutti i fogli
  - Premi **Invio** per cercare
  - Premi **Invio** di nuovo per andare al risultato successivo
  - Navigazione ciclica tra i risultati

### Funzionalità Avanzate
- **Lock Multi-utente**: Solo un utente può modificare i dati alla volta
- **Modalità Sola Lettura**: Apertura automatica in sola lettura se già in uso
- **Salvataggio Automatico**: I dati vengono salvati automaticamente dopo 2 secondi dall'ultima modifica
- **Esportazione Excel**: Esportazione dei fogli in formato Excel (.xlsx)
- **Logging**: Tracciamento completo delle operazioni

### Colonne Standard
0. **#** - Numero riga (automatico, non modificabile)
1. Tipo asset
2. Marca
3. Modello
4. Seriale
5. Rif inv biofer
6. Descrizione
7. Causa dismissione (campo dropdown configurabile)

## Menu Organizzati per Funzione

L'applicazione è organizzata in menu logici per funzione:

### 📁 File
- **Nuovo Foglio** (Ctrl+N) - Crea un nuovo foglio di dismissione
- **Salva** (Ctrl+S) - Salva manualmente il foglio corrente
- **Esporta in Excel** (Ctrl+E) - Esporta il foglio in formato Excel

### 📝 Riga
- **Aggiungi Riga** (Ctrl+R) - Aggiunge una nuova riga al foglio
- **Rimuovi Riga** (Ctrl+Delete) - Rimuove la riga selezionata

### 📊 Colonna
- **Aggiungi Colonna** (Ctrl+K) - Aggiunge una nuova colonna personalizzata
- **Rimuovi Colonna** (Ctrl+Shift+Delete) - Rimuove la colonna selezionata

### 📄 Foglio
- **Rinomina Foglio** (F2) - Rinomina il foglio corrente
- **Elimina Foglio** (Ctrl+D) - Elimina definitivamente il foglio

### 📦 Archiviazione
- **Archivia Foglio Corrente** (Ctrl+A) - Archivia il foglio corrente
- **Recupera Foglio da Archivio** (Ctrl+H) - Visualizza e ripristina fogli archiviati

### 🔧 Strumenti
- **Gestione Colonne** - Menu per configurare colonne con dropdown
  - **Causa Dismissione** (Ctrl+O) - Gestisce le opzioni del dropdown "Causa dismissione"
  - *(Altre colonne future potranno essere aggiunte qui)*

### 🔍 Ricerca
- **Barra di ricerca** - Campo di testo in alto a destra
- **Cerca** - Avvia la ricerca
- **► (Successivo)** - Va al risultato successivo
- **Invio nella barra** - Cerca o va al successivo se ci sono già risultati

## Tecnologie Utilizzate

- **.NET 8.0** (Windows Forms)
- **Newtonsoft.Json** - Serializzazione/deserializzazione JSON
- **ClosedXML** - Esportazione file Excel

## Struttura Dati

I dati vengono salvati nella cartella `data/` nella directory dell'eseguibile:
- `data/*.json` - Fogli attivi
- `data/archived/*.json` - Fogli archiviati
- `data/config/settings.json` - Impostazioni applicazione
- `data/config/lock.json` - File di lock
- `data/application.log` - Log applicazione

## Compilazione

### Requisiti
- .NET 8.0 SDK o superiore
- Windows OS (per Windows Forms)
- Visual Studio 2022 (opzionale, consigliato)

### Metodo 1: Visual Studio (Consigliato)
1. Apri il file `GestioneCespiti.sln` con Visual Studio 2022
2. Premi F5 per compilare ed eseguire
3. Oppure usa Build → Build Solution (Ctrl+Shift+B)

### Metodo 2: Command Line
```bash
# Dalla directory OnlyCespiti
dotnet restore
dotnet build
dotnet run
```

### Metodo 3: PowerShell Script
```powershell
# Dalla directory OnlyCespiti
.\Scripts\build.ps1
```

### Risoluzione Problemi
Se ricevi errore "non contiene alcun file di progetto o di soluzione":
- Assicurati di essere nella directory `OnlyCespiti`
- Verifica che esistano `GestioneCespiti.sln` e `GestioneCespiti.csproj`
- Usa il percorso completo: `dotnet build C:\percorso\OnlyCespiti\GestioneCespiti.sln`

## Note di Sviluppo

### Pattern Architetturali
- **Separation of Concerns**: Separazione tra UI (Forms), Logica (Services) e Dati (Models)
- **Service Layer**: Servizi separati per ogni responsabilità
- **Defensive Programming**: Validazione input, gestione errori, logging

### Best Practices Implementate
- Dispose pattern per risorse (Timer, DataTable, Streams)
- Lock pattern per thread safety
- File transazionali per salvataggio sicuro (file.tmp → file, con backup)
- Rotazione automatica log quando supera 10MB

## Miglioramenti Recenti

### Riorganizzazione Codice (2025-12-07)
1. ✅ Separazione dialogs in file distinti
2. ✅ Spostamento SearchResult nel namespace Models
3. ✅ Rinomina MainForm_Designer.cs → MainForm.Designer.cs (convenzione .NET)
4. ✅ Organizzazione cartelle per tipo (Forms, Models, Services)
5. ✅ Creazione cartelle Properties e Scripts
6. ✅ Aggiunta .gitignore

### Miglioramenti UX e Menu (2025-12-07)
1. ✅ **Numerazione Righe**: Aggiunta colonna "#" automatica con numerazione progressiva
2. ✅ **Ricerca Migliorata**: Premi Invio per cercare o andare al risultato successivo
3. ✅ **Riorganizzazione Menu**: Menu divisi logicamente per funzione
   - File: operazioni su file
   - Riga: gestione righe
   - Colonna: gestione colonne
   - Foglio: operazioni sui fogli
   - Archiviazione: gestione archivio
   - Strumenti: configurazione avanzata
4. ✅ **Menu Gestione Colonne**: Sottomenu strutturato per future colonne con dropdown
5. ✅ **UI Migliorata**: Tooltip informativi e simboli più chiari (► per successivo)

## Licenza

Proprietario: OnlyCespiti
Uso interno aziendale
