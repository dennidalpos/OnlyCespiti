using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GestioneCespiti.Models;
using Newtonsoft.Json;

namespace GestioneCespiti.Services
{
    public class DataPersistenceService
    {
        private readonly string _dataFolder;
        private readonly string _archivedFolder;

        public DataPersistenceService()
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            _dataFolder = Path.Combine(exePath, "data");
            _archivedFolder = Path.Combine(_dataFolder, "archived");

            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }

            if (!Directory.Exists(_archivedFolder))
            {
                Directory.CreateDirectory(_archivedFolder);
            }
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
            string filePath = Path.Combine(targetFolder, sheet.FileName);

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
            string filePath = Path.Combine(targetFolder, sheet.FileName);
            
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

            string sourcePath = Path.Combine(_dataFolder, sheet.FileName);

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

            string sourcePath = Path.Combine(_archivedFolder, sheet.FileName);

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

            string safe = Regex.Replace(input, @"[^\w\s-]", "");
            safe = Regex.Replace(safe, @"\s+", "_");

            if (safe.Length > 50)
                safe = safe.Substring(0, 50);

            safe += "_" + DateTime.Now.ToString("yyyyMMddHHmmss");

            return safe;
        }
    }
}
