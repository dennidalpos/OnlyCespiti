using System.IO;
using Newtonsoft.Json;
using GestioneCespiti.Models;
using GestioneCespiti.Services;
using Xunit;

namespace GestioneCespiti.Tests;

public class DataPersistenceServiceTests
{
    [Fact]
    public void SaveAndLoadAllSheets_RoundTripsSheetData()
    {
        using var workspace = new TestWorkspace();
        var service = new DataPersistenceService();
        var sheet = AssetSheet.CreateNew("Inventario 2026");
        sheet.Rows.Add(new Asset());
        sheet.Rows[0]["Marca"] = "Contoso";
        sheet.Rows[0]["Tipo asset"] = "Notebook";

        service.SaveSheet(sheet);

        var loadedSheets = service.LoadAllSheets();

        var loaded = Assert.Single(loadedSheets);
        Assert.Equal("Inventario 2026", loaded.Header);
        Assert.Equal("Contoso", loaded.Rows[0]["Marca"]);
        Assert.Equal("Notebook", loaded.Rows[0]["Tipo asset"]);
        Assert.False(string.IsNullOrWhiteSpace(loaded.FileName));
    }

    [Fact]
    public void ImportSheetFromJson_NormalizesLegacyAndDuplicateColumns()
    {
        using var workspace = new TestWorkspace();
        var service = new DataPersistenceService();
        var importPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");

        try
        {
            File.WriteAllText(importPath,
                """
                {
                  "Header": "Import legacy",
                  "Columns": ["Tipo asset", "Rif inv biofer", "Rif inventario", "Descrizione", "Descrizione"],
                  "Rows": [
                    {
                      "Values": {
                        "Tipo asset": "Desktop",
                        "Rif inv biofer": "INV-001",
                        "Descrizione": "PC ufficio"
                      }
                    }
                  ]
                }
                """);

            var imported = service.ImportSheetFromJson(importPath);

            Assert.DoesNotContain(imported.Columns, column => column == "Rif inv biofer");
            Assert.Contains("Rif inventario", imported.Columns);
            Assert.Single(imported.Columns.FindAll(column => column == "Descrizione"));
            Assert.Equal("INV-001", imported.Rows[0]["Rif inventario"]);
        }
        finally
        {
            if (File.Exists(importPath))
            {
                File.Delete(importPath);
            }
        }
    }

    [Fact]
    public void ExportSheetToJson_OverwriteCreatesBackupAndCleansTempFile()
    {
        using var workspace = new TestWorkspace();
        var service = new DataPersistenceService();
        var exportPath = Path.Combine(workspace.DataDirectory, "export.json");

        var firstSheet = AssetSheet.CreateNew("Prima versione");
        firstSheet.Rows.Add(new Asset());
        firstSheet.Rows[0]["Descrizione"] = "Versione A";
        service.ExportSheetToJson(firstSheet, exportPath);

        var secondSheet = AssetSheet.CreateNew("Seconda versione");
        secondSheet.Rows.Add(new Asset());
        secondSheet.Rows[0]["Descrizione"] = "Versione B";
        service.ExportSheetToJson(secondSheet, exportPath);

        var current = JsonConvert.DeserializeObject<AssetSheet>(File.ReadAllText(exportPath));
        var backup = JsonConvert.DeserializeObject<AssetSheet>(File.ReadAllText(exportPath + ".bak"));

        Assert.NotNull(current);
        Assert.NotNull(backup);
        Assert.Equal("Seconda versione", current!.Header);
        Assert.Equal("Prima versione", backup!.Header);
        Assert.False(File.Exists(exportPath + ".tmp"));
    }

    [Fact]
    public void ArchiveAndUnarchiveSheet_MovesBackupWithSheet()
    {
        using var workspace = new TestWorkspace();
        var service = new DataPersistenceService();
        var sheet = AssetSheet.CreateNew("Archivio backup");
        sheet.Rows.Add(new Asset());
        sheet.Rows[0]["Descrizione"] = "Prima versione";

        service.SaveSheet(sheet);
        sheet.Rows[0]["Descrizione"] = "Seconda versione";
        service.SaveSheet(sheet);

        var activePath = Path.Combine(workspace.DataDirectory, sheet.FileName);
        var activeBackupPath = activePath + ".bak";
        Assert.True(File.Exists(activePath));
        Assert.True(File.Exists(activeBackupPath));

        service.ArchiveSheet(sheet);

        var archivedPath = Path.Combine(workspace.DataDirectory, "archived", sheet.FileName);
        var archivedBackupPath = archivedPath + ".bak";
        Assert.False(File.Exists(activePath));
        Assert.False(File.Exists(activeBackupPath));
        Assert.True(File.Exists(archivedPath));
        Assert.True(File.Exists(archivedBackupPath));

        service.UnarchiveSheet(sheet);

        var restoredPath = Path.Combine(workspace.DataDirectory, sheet.FileName);
        var restoredBackupPath = restoredPath + ".bak";
        Assert.False(File.Exists(archivedPath));
        Assert.False(File.Exists(archivedBackupPath));
        Assert.True(File.Exists(restoredPath));
        Assert.True(File.Exists(restoredBackupPath));
    }

    [Fact]
    public void DeleteSheet_RemovesMainFileAndBackup()
    {
        using var workspace = new TestWorkspace();
        var service = new DataPersistenceService();
        var sheet = AssetSheet.CreateNew("Delete backup");
        sheet.Rows.Add(new Asset());
        sheet.Rows[0]["Descrizione"] = "Prima versione";

        service.SaveSheet(sheet);
        sheet.Rows[0]["Descrizione"] = "Seconda versione";
        service.SaveSheet(sheet);

        var filePath = Path.Combine(workspace.DataDirectory, sheet.FileName);
        var backupPath = filePath + ".bak";
        Assert.True(File.Exists(filePath));
        Assert.True(File.Exists(backupPath));

        service.DeleteSheet(sheet);

        Assert.False(File.Exists(filePath));
        Assert.False(File.Exists(backupPath));
    }
}
