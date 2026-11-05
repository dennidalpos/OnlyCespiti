# OnlyCespiti

## Obiettivi

- Mantenere un'app desktop Windows per la gestione di fogli di dismissione cespiti.
- Consentire creazione, modifica, archiviazione, ricerca ed esportazione dei fogli.
- Garantire persistenza locale affidabile dei dati JSON e export Excel.

## Architettura

- Applicazione Windows Forms .NET 8 (`GestioneCespiti.csproj`).
- Entry point in `Program.cs` con avvio di `MainForm`.
- UI e dialog in `Forms/`.
- Coordinamento comportamento UI in `Managers/`.
- Modello dominio in `Models/`.
- Servizi infrastrutturali in `Services/`.
- Utility condivise in `Utils/`.
- Script di supporto build/clean in `Scripts/`.
- Test automatici in `tests/`.

## Comportamento atteso

- L'applicazione apre e gestisce più fogli locali serializzati in JSON.
- Le modifiche alle celle vengono autosalvate con debounce e con meccanismi di backup.
- La ricerca attraversa fogli attivi e, opzionalmente, archiviati.
- I fogli archiviati aperti dalla ricerca sono consultabili in sola lettura fino a ripristino esplicito.
- L'export in Excel produce file `.xlsx` dal foglio corrente.
- Un lock applicativo impedisce modifiche concorrenti da più istanze.
- In assenza di lock l'app resta consultabile in sola lettura.

## Vincoli

- Stack target: Windows 10/11 e .NET 8 con Windows Forms.
- Il repository si chiama `OnlyCespiti`, mentre nome assembly/applicazione resta `GestioneCespiti`.
- La persistenza prevista è locale su file; non è definito alcun backend remoto.
- Gli script di clean devono riportare la repo a uno stato locale pulito senza toccare i file versionati.
