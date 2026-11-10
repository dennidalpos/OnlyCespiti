using System;
using System.IO;
using ClosedXML.Excel;
using GestioneCespiti.Models;
using GestioneCespiti.Utils;

namespace GestioneCespiti.Services
{
    public class ExcelExportService
    {
        public void ExportToExcel(AssetSheet sheet, string filePath)
        {
            if (sheet == null)
                throw new ArgumentNullException(nameof(sheet));

            if (sheet.Columns == null || sheet.Columns.Count == 0)
                throw new ArgumentException("Il foglio non contiene colonne", nameof(sheet));

            if (sheet.Rows == null)
                throw new ArgumentException("Il foglio non ha un elenco di righe valido", nameof(sheet));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Il percorso del file non può essere vuoto", nameof(filePath));

            string? directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("Directory path cannot be determined", nameof(filePath));

            try
            {
                string fullDirectory = Path.GetFullPath(directory);
                PathValidator.EnsureDirectoryExists(fullDirectory);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Percorso file non valido: {ex.Message}", nameof(filePath), ex);
            }

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add(BuildSafeWorksheetName(sheet.Header));

                    for (int col = 0; col < sheet.Columns.Count; col++)
                    {
                        var cell = worksheet.Cell(1, col + 1);
                        cell.Value = sheet.Columns[col];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    for (int row = 0; row < sheet.Rows.Count; row++)
                    {
                        var asset = sheet.Rows[row];
                        for (int col = 0; col < sheet.Columns.Count; col++)
                        {
                            string columnName = sheet.Columns[col];
                            string value = asset[columnName];
                            var cell = worksheet.Cell(row + 2, col + 1);
                            cell.Value = value;
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        }
                    }

                    worksheet.Columns().AdjustToContents();
                    worksheet.SheetView.FreezeRows(1);

                    string extension = Path.GetExtension(filePath);
                    string tempFilePath = filePath + ".tmp" + extension;

                    try
                    {
                        workbook.SaveAs(tempFilePath);

                        if (File.Exists(filePath))
                        {
                            var backupFilePath = filePath + ".bak";
                            File.Copy(filePath, backupFilePath, true);
                        }

                        File.Move(tempFilePath, filePath, true);
                        Logger.LogInfo($"Export Excel completato: {filePath}");
                    }
                    finally
                    {
                        if (File.Exists(tempFilePath))
                        {
                            File.Delete(tempFilePath);
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                Logger.LogError($"Errore I/O durante export Excel: {filePath}", ex);
                throw new IOException("Impossibile salvare il file Excel. Il file potrebbe essere aperto in un altro programma.", ex);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Errore durante export Excel: {filePath}", ex);
                throw new Exception($"Errore durante la creazione del file Excel: {ex.Message}", ex);
            }
        }

        private static string BuildSafeWorksheetName(string header)
        {
            const int maxLen = 31;
            var invalidChars = new[] { ':', '\\', '/', '?', '*', '[', ']' };

            string name = string.IsNullOrWhiteSpace(header) ? "Cespiti" : header.Trim();
            foreach (var ch in invalidChars)
            {
                name = name.Replace(ch, '_');
            }

            if (name.Length > maxLen)
            {
                name = name.Substring(0, maxLen);
            }

            return string.IsNullOrWhiteSpace(name) ? "Cespiti" : name;
        }
    }
}
