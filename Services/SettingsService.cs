using System;
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

        public SettingsService()
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string dataFolder = Path.Combine(exePath, "data");
            _configFolder = Path.Combine(dataFolder, "config");
            _settingsFile = Path.Combine(_configFolder, "settings.json");

            if (!Directory.Exists(_configFolder))
            {
                Directory.CreateDirectory(_configFolder);
            }
        }

        public AppSettings LoadSettings()
        {
            if (!File.Exists(_settingsFile))
            {
                var defaultSettings = new AppSettings();
                SaveSettings(defaultSettings);
                return defaultSettings;
            }

            try
            {
                string json = File.ReadAllText(_settingsFile, Encoding.UTF8);
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    Logger.LogWarning("File settings vuoto, uso impostazioni di default");
                    return new AppSettings();
                }

                var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                
                if (settings != null)
                {
                    if (settings.CauseDismissioneOptions == null)
                    {
                        Logger.LogWarning("CauseDismissioneOptions null, inizializzazione con valori default");
                        settings.CauseDismissioneOptions = new AppSettings().CauseDismissioneOptions;
                    }

                    settings.CauseDismissioneOptions = SanitizeOptions(settings.CauseDismissioneOptions);
                    return settings;
                }
                
                Logger.LogWarning("Deserializzazione settings fallita, uso impostazioni di default");
                return new AppSettings();
            }
            catch (JsonException jsonEx)
            {
                Logger.LogError("Errore JSON caricamento settings", jsonEx);
                return new AppSettings();
            }
            catch (IOException ioEx)
            {
                Logger.LogError("Errore I/O caricamento settings", ioEx);
                return new AppSettings();
            }
            catch (Exception ex)
            {
                Logger.LogError("Errore generico caricamento settings", ex);
                return new AppSettings();
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            try
            {
                if (settings.CauseDismissioneOptions == null)
                {
                    settings.CauseDismissioneOptions = new AppSettings().CauseDismissioneOptions;
                    Logger.LogWarning("CauseDismissioneOptions era null, inizializzato con valori default");
                }

                settings.CauseDismissioneOptions = SanitizeOptions(settings.CauseDismissioneOptions);

                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);

                string tempFile = _settingsFile + ".tmp";
                File.WriteAllText(tempFile, json, Encoding.UTF8);

                if (File.Exists(_settingsFile))
                {
                    string backupFile = _settingsFile + ".bak";
                    File.Copy(_settingsFile, backupFile, true);
                }

                File.Move(tempFile, _settingsFile, true);

                if (File.Exists(_settingsFile + ".bak"))
                {
                    File.Delete(_settingsFile + ".bak");
                }

                Logger.LogInfo("Impostazioni salvate con successo");
            }
            catch (Exception ex)
            {
                Logger.LogError("Errore salvataggio impostazioni", ex);
                throw new IOException("Impossibile salvare le impostazioni", ex);
            }
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
