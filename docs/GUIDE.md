# OnlyCespiti user and developer guide

[Italian guide](GUIDE.it.md)

GestioneCespiti is a Windows Forms app for asset-disposal sheets with local JSON storage, cross-sheet search, archives and Excel export. The repository is OnlyCespiti; project and assembly names remain GestioneCespiti.

## Build, run and test

Use Windows 10/11, a compatible .NET SDK targeting .NET 8 and PowerShell for scripts.

~~~powershell
dotnet restore GestioneCespiti.sln
dotnet build GestioneCespiti.sln -c Release
dotnet run --project GestioneCespiti.csproj
dotnet test GestioneCespiti.sln -c Release
~~~

For an explicitly chosen publish without automatic cleanup:

~~~powershell
./Scripts/build.ps1 -NoClean -PublishMode SelfContained
~~~

PublishMode supports SelfContained or FrameworkDependent. Without an explicit mode the script prompts. Runtime supports win-x64/win-x86/win-arm64; configuration supports Debug/Release. NoPublish builds without publishing, SkipRestore reuses prior restore, and KeepPublishOnClean only controls publish retention. These switches do not authorize a broad cleanup.

The build script cleans by default. clean.ps1 recursively includes data, caches and .bak/.log/.tmp files; KeepPublish does not protect data. Do not run it on a directory containing personal sheets, backups or retained test evidence without a concrete authorized scope. Tests/builds for repository agents belong in isolated central-runtime fixtures.

## Files and reliable saving

Runtime data normally lives in data beside the executable:

| Path | Content |
| --- | --- |
| data/*.json | Active sheets |
| data/archived/*.json | Archived sheets |
| data/config/settings.json | Global settings |
| data/config/sheets/*.json | Per-sheet settings |

Persistence writes a temporary file and replaces the destination, keeping backups for overwrites. Reading a corrupted main JSON attempts backup recovery. JSON import/export and Excel export use their own normalization/safe-write handling. Keep recovery copies until the result is verified; a backup is not permission to delete originals.

## Sheets, rows and columns

Create, rename, delete or archive sheets. The archive supports explicit restore/permanent deletion. Add/remove rows and custom columns; standard columns cannot be removed and are visually distinct. Controlled values support disposal-reason/asset-type fields.

Changes update the in-memory model and enter the autosave queue. Debounce is two seconds; failed writes remain pending for later attempts. Wait for visible outcomes and investigate errors before assuming data is durable.

## Search

Global search has Include archived and Match case toggles. Enter runs search or advances to the next result. Changing a filter while text is present reruns search. Emptying the search text resets results/highlights. Result navigation focuses its sheet, row and column.

Archived results can be viewed but remain read-only until restored through the archive; opening a search result is not a restore.

## Import and export

Export the current sheet as XLSX or JSON. Import JSON normalizes rows/columns and allocates a unique internal filename. Initial names are sanitized; overwritten export destinations retain their safe-write/backup handling. Verify external spreadsheet rendering separately when it matters.

## Concurrency and logging

If the application cannot acquire its lock, modifying commands are disabled and title/status identify read-only mode. Reading/search remain available. A local lock test is not proof of every SMB deployment; qualify the selected shared environment separately.

Logging records application events, handled/unhandled errors, inconsistent inputs and import/export/save results. Real logs can include user data and stay outside the published source.

See [README](../README.md), [specification](../PROJECT_SPEC.md), [LICENSE](../LICENSE) and [security policy](../SECURITY.md).
