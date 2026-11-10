using System.IO;
using ClosedXML.Excel;
using GestioneCespiti.Models;
using GestioneCespiti.Services;
using Xunit;

namespace GestioneCespiti.Tests;

public class ExcelExportServiceTests
{
    [Fact]
    public void ExportToExcel_OverwriteCreatesBackupAndCleansTempFile()
    {
        using var workspace = new TestWorkspace();
        var service = new ExcelExportService();
        var exportPath = Path.Combine(workspace.DataDirectory, "cespiti.xlsx");

        var firstSheet = AssetSheet.CreateNew("Export A");
        firstSheet.Rows.Add(new Asset());
        firstSheet.Rows[0]["Descrizione"] = "Versione A";
        service.ExportToExcel(firstSheet, exportPath);

        var secondSheet = AssetSheet.CreateNew("Export B");
        secondSheet.Rows.Add(new Asset());
        secondSheet.Rows[0]["Descrizione"] = "Versione B";
        service.ExportToExcel(secondSheet, exportPath);

        using var currentWorkbook = new XLWorkbook(exportPath);
        using var backupStream = File.OpenRead(exportPath + ".bak");
        using var backupWorkbook = new XLWorkbook(backupStream);

        var currentSheet = currentWorkbook.Worksheet(1);
        var backupSheet = backupWorkbook.Worksheet(1);

        Assert.Equal("Descrizione", currentSheet.Cell(1, 6).GetString());
        Assert.Equal("Versione B", currentSheet.Cell(2, 6).GetString());
        Assert.Equal("Versione A", backupSheet.Cell(2, 6).GetString());
        Assert.False(File.Exists(exportPath + ".tmp" + Path.GetExtension(exportPath)));
    }
}
