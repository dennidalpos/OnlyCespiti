# OnlyCespiti

Windows Forms desktop app (`GestioneCespiti`) to manage asset disposal sheets. Sheets are stored as local JSON files, searchable across all sheets and exportable to Excel.

## Features

- **Multiple sheets**: create, rename, delete and archive sheets; restore or permanently delete archived ones.
- **Standard and custom columns**: add or remove rows and custom columns; standard columns are protected and visually distinct. Controlled options for fields such as disposal reason and asset type.
- **Global search**: search across all sheets, optionally including archived ones and with case-sensitive matching; jump to the matching tab, row and column.
- **Import and export**: export the current sheet to `.xlsx` (ClosedXML) or JSON; import a sheet from JSON with column and row normalization.
- **Reliable saving**: debounced autosave (2 s), atomic file replacement, `.bak` backups and automatic recovery from backup when a JSON file is corrupted.
- **Concurrency lock**: an application lock prevents concurrent edits from multiple instances; without the lock the app opens read-only.
- **Local data only**: no backend; data lives in a `data` folder next to the executable.

## Requirements

- Windows 10/11
- .NET SDK 8.0 to build
- PowerShell 7+ recommended for the build scripts

## Build and run

```powershell
dotnet restore
dotnet build -c Release
dotnet run
```

Run the tests:

```powershell
dotnet test GestioneCespiti.sln -c Release
```

Produce a publish folder (win-x64, self-contained single file by default):

```powershell
./Scripts/build.ps1 -PublishMode SelfContained
```

`./Scripts/clean.ps1` removes build output, IDE caches and local runtime data.

## Repository layout

| Path | Content |
| --- | --- |
| `Forms/`, `Managers/` | UI, dialogs, grid, search and status logic |
| `Services/`, `Models/`, `Utils/` | Persistence, lock, Excel export, settings, logging and domain models |
| `Scripts/` | Build and clean automation |
| `tests/` | Automated tests for services and managers |
| `tools/` | Bundled third-party tooling (WiX Toolset 3.14, NSSM) |

The repository is named `OnlyCespiti`; the .NET project and assembly keep the name `GestioneCespiti`.

## Documentation

The detailed guide is written in Italian: [docs/GUIDE.it.md](docs/GUIDE.it.md). See also [PROJECT_SPEC.md](PROJECT_SPEC.md).

## Security

Data stays on the local machine or on a shared folder you choose. See [SECURITY.md](SECURITY.md) to report a vulnerability.

## License

Proprietary, all rights reserved. See [LICENSE](LICENSE). Bundled tools under `tools/` keep their own licenses.
