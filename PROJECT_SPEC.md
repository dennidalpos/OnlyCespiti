# Product specification

[Retained Italian source](PROJECT_SPEC.it.md)

OnlyCespiti is the repository for the Windows Forms application/assembly GestioneCespiti, targeting net8.0-windows.

## Responsibilities

Forms contains UI/dialogs; Managers coordinates grids/search/state; Models contains domain types; Services implements persistence, locking, export, settings and logging; Utils contains shared helpers. Program starts MainForm. Scripts supplies build/clean automation; tests covers selected services/managers.

## Current behavior

Users maintain multiple JSON asset-disposal sheets. Cells autosave with a two-second debounce and backup handling. Search spans active sheets and optionally archived ones. Archived search results stay read-only until explicitly restored. Excel export writes the current sheet to .xlsx.

An application lock prevents concurrent editing; without it the app remains readable in read-only mode. There is no remote backend. Runtime data normally lives beside the executable, and a user-selected shared directory is a separate environmental case.

## Operational limits

Windows 10/11 and compatible .NET SDK/runtime are required. Scripts/build.ps1 cleans by default; Scripts/clean.ps1 recursively includes data and backup/log files in its deletion scope. This is not a safe default for a checkout containing real data or retained evidence. Preserve data and use NoClean when building without an explicitly approved cleanup.

A successful service test does not qualify native UI, actual Excel software or multi-host shared-folder behavior. See the [English guide](docs/GUIDE.md) and retained [Italian guide](docs/GUIDE.it.md). No code or tracker status is changed by a documentation audit.
