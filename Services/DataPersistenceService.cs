using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GestioneCespiti.Models;
using GestioneCespiti.Utils;
using Newtonsoft.Json;

namespace GestioneCespiti.Services
{
    public class DataPersistenceService
    {
        private readonly string _dataFolder;
        private readonly string _archivedFolder;
        private static readonly Regex InvalidCharsRegex = new Regex(@"[^\w\s-]", RegexOptions.Compiled);
        private static readonly Regex WhitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);

        public DataPersistenceService()
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            _dataFolder = Path.GetFullPath(Path.Combine(exePath, "data"));
            _archivedFolder = Path.GetFullPath(Path.Combine(_dataFolder, "archived"));

            PathValidator.EnsureDirectoryExists(_dataFolder);
            PathValidator.EnsureDirectoryExists(_archivedFolder);
        }

        public List<AssetSheet> LoadAllSheets(bool includeArchived = false)
        {
            var sheets = new List<AssetSheet>();

            sheets.AddRange(LoadSheetsFromFolder(_dataFolder, false));

            if (includeArchived)
            {
                sheets.AddRange(LoadSheetsFromFolder(_archivedFolder, true));
            }

            return sheets;
        }

        private List<AssetSheet> LoadSheetsFromFolder(string folder, bool isArchived)
        {
            var sheets = new List<AssetSheet>();

            if (!Directory.Exists(folder))
                return sheets;

            var jsonFiles = Directory.GetFiles(folder, "*.json");

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        Logger.LogWarning($"File non trovato: {filePath}");
                        continue;
                    }

                    string json = File.ReadAllText(filePath, Encoding.UTF8);

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Logger.LogWarning($"File vuoto: {filePath}");
                        continue;
                    }

                    var sheet = JsonConvert.DeserializeObject<AssetSheet>(json);

                    if (sheet == null)
                    {
                        Logger.LogError($"Impossibile deserializzare il file: {filePath}. JSON potrebbe essere corrotto.");
                        continue;
                    }

                    if (sheet.Columns == null)
                    {
                        Logger.LogError($"File corrotto (Columns null): {filePath}");
                        continue;
                    }

                    if (sheet.Rows == null)
                    {
                        Logger.LogWarning($"File senza righe (Rows null): {filePath}. Inizializzazione con lista vuota.");
                        sheet.Rows = new List<Asset>();
                    }

                    NormalizeRows(sheet, filePath);
                    NormalizeColumns(sheet);
                    RemoveDuplicateColumns(sheet, filePath);

                    sheet.FileName = Path.GetFileName(filePath);
                    sheet.IsArchived = isArchived;
                    sheets.Add(sheet);
                }
                catch (JsonException jsonEx)
                {
                    Logger.LogError($"Errore JSON caricamento {filePath}", jsonEx);
                }
                catch (IOException ioEx)
                {
                    Logger.LogError($"Errore I/O caricamento {filePath}", ioEx);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Errore generico caricamento {filePath}", ex);
                }
            }

            return sheets;
        }

        public void SaveSheet(AssetSheet sheet)
        {
            if (sheet == null)
                throw new ArgumentNullException(nameof(sheet));

            if (string.IsNullOrWhiteSpace(sheet.FileName))
            {
                sheet.FileName = GenerateSafeFileName(sheet.Header) + ".json";
            }

            string targetFolder = sheet.IsArchived ? _archivedFolder : _dataFolder;
            string filePath = PathValidator.ValidateAndGetSafePath(targetFolder, sheet.FileName);

            try
            {
                string tempFile = filePath + ".tmp";
                string json = JsonConvert.SerializeObject(sheet, Formatting.Indented);
                File.WriteAllText(tempFile, json, Encoding.UTF8);

                if (File.Exists(filePath))
                {
                    string backupFile = filePath + ".bak";
                    File.Copy(filePath, backupFile, true);
                }

                File.Move(tempFile, filePath, true);

                if (File.Exists(filePath + ".bak"))
                {
                    File.Delete(filePath + ".bak");
                }

                Logger.LogInfo($"Foglio salvato: {sheet.Header} -> {filePath}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Errore salvataggio foglio '{sheet.Header}'", ex);
                throw;
            }
        }

        public void DeleteSheet(AssetSheet sheet)
        {
            if (sheet == null || string.IsNullOrWhiteSpace(sheet.FileName))
                return;

            string targetFolder = sheet.IsArchived ? _archivedFolder : _dataFolder;
            string filePath = PathValidator.ValidateAndGetSafePath(targetFolder, sheet.FileName);
            
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Logger.LogInfo($"Foglio eliminato: {sheet.Header} ({filePath})");
                }
                else
                {
                    Logger.LogWarning($"Tentativo di eliminazione foglio inesistente: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Errore eliminazione foglio '{sheet.Header}'", ex);
                throw;
            }
        }

        public void ArchiveSheet(AssetSheet sheet)
        {
            if (sheet == null || sheet.IsArchived)
                return;

            string sourcePath = PathValidator.ValidateAndGetSafePath(_dataFolder, sheet.FileName);

            if (!File.Exists(sourcePath))
            {
                Logger.LogWarning($"File sorgente non trovato per archiviazione: {sourcePath}");
                return;
            }

            try
            {
                string destPath = GetUniqueFileName(_archivedFolder, sheet.FileName);
                sheet.FileName = Path.GetFileName(destPath);

                File.Move(sourcePath, destPath);
                sheet.IsArchived = true;
                Logger.LogInfo($"Foglio archiviato: {sheet.Header} -> {destPath}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Errore archiviazione foglio '{sheet.Header}'", ex);
                throw;
            }
        }

        public void UnarchiveSheet(AssetSheet sheet)
        {
            if (sheet == null || !sheet.IsArchived)
                return;

            string sourcePath = PathValidator.ValidateAndGetSafePath(_archivedFolder, sheet.FileName);

            if (!File.Exists(sourcePath))
            {
                Logger.LogWarning($"File archiviato non trovato: {sourcePath}");
                return;
            }

            try
            {
                string destPath = GetUniqueFileName(_dataFolder, sheet.FileName);
                sheet.FileName = Path.GetFileName(destPath);

                File.Move(sourcePath, destPath);
                sheet.IsArchived = false;
                Logger.LogInfo($"Foglio ripristinato: {sheet.Header} -> {destPath}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Errore ripristino foglio '{sheet.Header}'", ex);
                throw;
            }
        }

        private string GetUniqueFileName(string targetFolder, string originalFileName)
        {
            string destPath = Path.Combine(targetFolder, originalFileName);

            if (!File.Exists(destPath))
                return destPath;

            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
            string extension = Path.GetExtension(originalFileName);
            int counter = 1;

            do
            {
                destPath = Path.Combine(targetFolder, $"{fileNameWithoutExt}_{counter}{extension}");
                counter++;

                if (counter > 1000)
                {
                    throw new InvalidOperationException("Troppi file con lo stesso nome");
                }
            } while (File.Exists(destPath));

            return destPath;
        }

        private string GenerateSafeFileName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                input = "foglio";

            string safe = InvalidCharsRegex.Replace(input, "");
            safe = WhitespaceRegex.Replace(safe, "_");

            if (safe.Length > 50)
                safe = safe.Substring(0, 50);

            safe += "_" + DateTime.Now.ToString("yyyyMMddHHmmss");

            return safe;
        }

        private static void NormalizeColumns(AssetSheet sheet)
        {
            const string legacyColumn = "Rif inv biofer";
            const string updatedColumn = "Rif inventario";

            if (sheet.Columns == null)
                return;

            bool hasLegacy = sheet.Columns.Any(c => c.Equals(legacyColumn, StringComparison.OrdinalIgnoreCase));
            if (!hasLegacy)
                return;

            bool hasUpdated = sheet.Columns.Any(c => c.Equals(updatedColumn, StringComparison.OrdinalIgnoreCase));

            for (int i = 0; i < sheet.Columns.Count; i++)
            {
                if (sheet.Columns[i].Equals(legacyColumn, StringComparison.OrdinalIgnoreCase))
                {
                    if (!hasUpdated)
                    {
                        sheet.Columns[i] = updatedColumn;
                    }
                    else
                    {
                        sheet.Columns.RemoveAt(i);
                        i--;
                    }
                }
            }

            if (sheet.Rows == null)
                return;

            foreach (var asset in sheet.Rows)
            {
                if (asset.Values == null)
                {
                    asset.Values = new Dictionary<string, string>();
                }

                if (!asset.Values.TryGetValue(legacyColumn, out var legacyValue))
                    continue;

                if (!asset.Values.ContainsKey(updatedColumn))
                {
                    asset.Values[updatedColumn] = legacyValue;
                }

                asset.Values.Remove(legacyColumn);
            }
        }

        private static void NormalizeRows(AssetSheet sheet, string filePath)
        {
            if (sheet.Rows == null)
            {
                sheet.Rows = new List<Asset>();
                return;
            }

            int initialCount = sheet.Rows.Count;
            sheet.Rows = sheet.Rows.Where(asset => asset != null).ToList();

            if (sheet.Rows.Count != initialCount)
            {
                Logger.LogWarning($"Righe nulle rimosse dal foglio: {filePath}");
            }

            foreach (var asset in sheet.Rows)
            {
                if (asset.Values == null)
                {
                    asset.Values = new Dictionary<string, string>();
                }
            }
        }

        private static void RemoveDuplicateColumns(AssetSheet sheet, string filePath)
        {
            if (sheet.Columns == null || sheet.Columns.Count == 0)
                return;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < sheet.Columns.Count; i++)
            {
                string column = sheet.Columns[i];
                if (seen.Add(column))
                {
                    continue;
                }

                Logger.LogWarning($"Colonna duplicata rimossa '{column}' nel file: {filePath}");
                sheet.Columns.RemoveAt(i);
                i--;

                if (sheet.Rows == null)
                    continue;

                // Rimuove i valori dal dizionario solo se la colonna non esiste più
                // (evita perdita dati quando sono presenti colonne duplicate con lo stesso nome).
                bool stillPresent = sheet.Columns.Any(c => c.Equals(column, StringComparison.OrdinalIgnoreCase));
                if (stillPresent)
                    continue;

                foreach (var asset in sheet.Rows)
                {
                    asset.Values?.Remove(column);
                }
            }
        }
    }
}
