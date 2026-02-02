using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GestioneCespiti.Models;
using Newtonsoft.Json;

namespace GestioneCespiti.Services
{
    public class SettingsService
    {
        private readonly string _configFolder;
        private readonly string _settingsFile;
        private readonly string _sheetSettingsFolder;

        public SettingsService()
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string dataFolder = Path.Combine(exePath, "data");
            _configFolder = Path.Combine(dataFolder, "config");
            _settingsFile = Path.Combine(_configFolder, "settings.json");
            _sheetSettingsFolder = Path.Combine(_configFolder, "sheets");

            if (!Directory.Exists(_configFolder))
            {
                Directory.CreateDirectory(_configFolder);
            }

            if (!Directory.Exists(_sheetSettingsFolder))
            {
                Directory.CreateDirectory(_sheetSettingsFolder);
            }
        }

        public AppSettings LoadSettings()
        {
            return LoadSettingsFromFile(_settingsFile, "settings", new AppSettings());
        }

        public void SaveSettings(AppSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            SaveSettingsToFile(settings, _settingsFile, "settings");
        }

        public AppSettings LoadSettingsForSheet(string sheetFileName, AppSettings defaultSettings)
        {
            if (string.IsNullOrWhiteSpace(sheetFileName))
                return CreateCopy(defaultSettings);

            string sheetSettingsFile = GetSheetSettingsFile(sheetFileName);
            return LoadSettingsFromFile(sheetSettingsFile, "settings foglio", CreateCopy(defaultSettings));
        }

        public void SaveSettingsForSheet(AppSettings settings, string sheetFileName)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (string.IsNullOrWhiteSpace(sheetFileName))
                throw new ArgumentException("Sheet file name cannot be empty", nameof(sheetFileName));

            string sheetSettingsFile = GetSheetSettingsFile(sheetFileName);
            SaveSettingsToFile(settings, sheetSettingsFile, "settings foglio");
        }

        private string GetSheetSettingsFile(string sheetFileName)
        {
            string safeName = Path.GetFileNameWithoutExtension(sheetFileName);
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                safeName = safeName.Replace(invalidChar, '_');
            }

            if (string.IsNullOrWhiteSpace(safeName))
            {
                safeName = "foglio";
            }

            return Path.Combine(_sheetSettingsFolder, $"{safeName}.settings.json");
        }

        private AppSettings LoadSettingsFromFile(string settingsFile, string settingsLabel, AppSettings defaultSettings)
        {
            if (!File.Exists(settingsFile))
            {
                var initialSettings = CreateCopy(defaultSettings);
                SaveSettingsToFile(initialSettings, settingsFile, settingsLabel);
                return initialSettings;
            }

            try
            {
                string json = File.ReadAllText(settingsFile, Encoding.UTF8);
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    Logger.LogWarning($"File {settingsLabel} vuoto, uso impostazioni di default");
                    return CreateCopy(defaultSettings);
                }

                var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                
                if (settings != null)
                {
                    ApplyDefaults(settings);
                    return settings;
                }
                
                Logger.LogWarning($"Deserializzazione {settingsLabel} fallita, uso impostazioni di default");
                return CreateCopy(defaultSettings);
            }
            catch (JsonException jsonEx)
            {
                Logger.LogError($"Errore JSON caricamento {settingsLabel}", jsonEx);
                return CreateCopy(defaultSettings);
            }
            catch (IOException ioEx)
            {
                Logger.LogError($"Errore I/O caricamento {settingsLabel}", ioEx);
                return CreateCopy(defaultSettings);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Errore generico caricamento {settingsLabel}", ex);
                return CreateCopy(defaultSettings);
            }
        }

        private void SaveSettingsToFile(AppSettings settings, string settingsFile, string settingsLabel)
        {
            try
            {
                ApplyDefaults(settings);

                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);

                string tempFile = settingsFile + ".tmp";
                File.WriteAllText(tempFile, json, Encoding.UTF8);

                if (File.Exists(settingsFile))
                {
                    string backupFile = settingsFile + ".bak";
                    File.Copy(settingsFile, backupFile, true);
                }

                File.Move(tempFile, settingsFile, true);

                if (File.Exists(settingsFile + ".bak"))
                {
                    File.Delete(settingsFile + ".bak");
                }

                Logger.LogInfo($"Impostazioni salvate con successo ({settingsLabel})");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Errore salvataggio {settingsLabel}", ex);
                throw new IOException("Impossibile salvare le impostazioni", ex);
            }
        }

        private static void ApplyDefaults(AppSettings settings)
        {
            if (settings.CauseDismissioneOptions == null)
            {
                settings.CauseDismissioneOptions = new AppSettings().CauseDismissioneOptions;
                Logger.LogWarning("CauseDismissioneOptions era null, inizializzato con valori default");
            }

            settings.CauseDismissioneOptions = SanitizeOptions(settings.CauseDismissioneOptions);
            settings.TipoAssetOptions ??= new List<string>();
            settings.TipoAssetOptions = SanitizeOptions(settings.TipoAssetOptions);
        }

        private static AppSettings CreateCopy(AppSettings settings)
        {
            return new AppSettings
            {
                CauseDismissioneOptions = new List<string>(settings.CauseDismissioneOptions ?? AppSettings.GetDefaultOptions()),
                TipoAssetOptions = new List<string>(settings.TipoAssetOptions ?? new List<string>())
            };
        }

        private static List<string> SanitizeOptions(List<string> options)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return options
                .Where(s => !string.IsNullOrWhiteSpace(s) && seen.Add(s))
                .ToList();
        }
    }
}
