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
- Aggiunta/rimozione righe (cespiti)
- Aggiunta/rimozione colonne personalizzate
- Modifica valori con salvataggio automatico
- Ricerca globale in tutti i fogli

### Funzionalità Avanzate
- **Lock Multi-utente**: Solo un utente può modificare i dati alla volta
- **Modalità Sola Lettura**: Apertura automatica in sola lettura se già in uso
- **Salvataggio Automatico**: I dati vengono salvati automaticamente dopo 2 secondi dall'ultima modifica
- **Esportazione Excel**: Esportazione dei fogli in formato Excel (.xlsx)
- **Logging**: Tracciamento completo delle operazioni

### Colonne Standard
1. Tipo asset
2. Marca
3. Modello
4. Seriale
5. Rif inv biofer
6. Descrizione
7. Causa dismissione (campo dropdown configurabile)

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
- .NET 8.0 SDK
- Windows OS (per Windows Forms)

### Build da riga di comando
```bash
dotnet build
```

### Build con PowerShell
```powershell
.\Scripts\build.ps1
```

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

## Licenza

Proprietario: OnlyCespiti
Uso interno aziendale
