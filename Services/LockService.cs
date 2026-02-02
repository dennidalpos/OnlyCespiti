using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using GestioneCespiti.Models;
using Newtonsoft.Json;

namespace GestioneCespiti.Services
{
    public class LockService
    {
        private readonly string _lockFile;
        private readonly string _currentUserName;
        private readonly string _currentHostName;
        private readonly string _currentProcessId;
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 100;

        public LockService()
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string dataFolder = Path.Combine(exePath, "data");
            string configFolder = Path.Combine(dataFolder, "config");

            if (!Directory.Exists(configFolder))
            {
                Directory.CreateDirectory(configFolder);
            }

            _lockFile = Path.Combine(configFolder, "lock.json");
            _currentUserName = Environment.UserName;
            _currentHostName = Environment.MachineName;
            _currentProcessId = Process.GetCurrentProcess().Id.ToString();
        }

        public bool TryAcquireLock()
        {
            return TryAcquireLockInternal(0);
        }

        private bool TryAcquireLockInternal(int retryCount)
        {
            FileStream? fs = null;
            try
            {
                fs = new FileStream(_lockFile,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);

                var lockData = new AppLock
                {
                    UserName = _currentUserName,
                    HostName = _currentHostName,
                    LockTime = DateTime.Now,
                    ProcessId = _currentProcessId
                };

                string json = JsonConvert.SerializeObject(lockData, Formatting.Indented);
                using (StreamWriter writer = new StreamWriter(fs, Encoding.UTF8))
                {
                    writer.Write(json);
                    writer.Flush();
                }

                fs.Dispose();
                fs = null;

                Logger.LogInfo($"Lock acquisito: {_currentUserName}@{_currentHostName} (PID: {_currentProcessId})");
                return true;
            }
            catch (IOException)
            {
                fs?.Dispose();

                if (retryCount >= MaxRetries)
                {
                    var existingLock = GetCurrentLock();
                    if (existingLock != null)
                    {
                        Logger.LogInfo($"Lock già attivo: {existingLock.UserName}@{existingLock.HostName} dal {existingLock.LockTime}");
                    }
                    return false;
                }

                System.Threading.Thread.Sleep(RetryDelayMs);

                if (!File.Exists(_lockFile))
                {
                    return TryAcquireLockInternal(retryCount + 1);
                }

                var currentLock = GetCurrentLock();
                if (currentLock != null && !IsProcessStillRunning(currentLock.ProcessId))
                {
                    Logger.LogWarning($"Lock stale trovato (processo {currentLock.ProcessId} non più attivo). Rimozione...");

                    try
                    {
                        File.Delete(_lockFile);
                        System.Threading.Thread.Sleep(RetryDelayMs);
                        return TryAcquireLockInternal(retryCount + 1);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Errore rimozione lock stale", ex);
                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                fs?.Dispose();
                Logger.LogError("Errore durante acquisizione lock", ex);
                return false;
            }
        }

        public AppLock? GetCurrentLock()
        {
            try
            {
                if (!File.Exists(_lockFile))
                    return null;

                string json = File.ReadAllText(_lockFile, Encoding.UTF8);
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    Logger.LogWarning("Lock file vuoto");
                    return null;
                }
                
                var lockData = JsonConvert.DeserializeObject<AppLock>(json);
                
                if (lockData == null)
                {
                    Logger.LogWarning("Lock file corrotto - deserializzazione fallita");
                    return null;
                }
                
                return lockData;
            }
            catch (JsonException jsonEx)
            {
                Logger.LogError("Lock file corrotto - JSON invalido", jsonEx);
                return null;
            }
            catch (IOException ioEx)
            {
                Logger.LogError("Errore lettura lock file", ioEx);
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError("Errore generico lettura lock", ex);
                return null;
            }
        }

        public void ReleaseLock()
        {
            try
            {
                if (!File.Exists(_lockFile))
                {
                    Logger.LogWarning("Tentativo di rilascio lock inesistente");
                    return;
                }
                
                if (!IsOwnLock())
                {
                    Logger.LogWarning("Tentativo di rilascio lock non proprio");
                    return;
                }

                File.Delete(_lockFile);
                Logger.LogInfo($"Lock rilasciato: {_currentUserName}@{_currentHostName}");
            }
            catch (Exception ex)
            {
                Logger.LogError("Errore durante rilascio lock", ex);
            }
        }

        public bool IsOwnLock()
        {
            var currentLock = GetCurrentLock();
            if (currentLock == null)
                return false;

            return currentLock.UserName == _currentUserName &&
                   currentLock.HostName == _currentHostName &&
                   currentLock.ProcessId == _currentProcessId;
        }

        private bool IsProcessStillRunning(string processIdStr)
        {
            if (string.IsNullOrWhiteSpace(processIdStr))
                return false;

            try
            {
                if (!int.TryParse(processIdStr, out int processId))
                {
                    Logger.LogWarning($"Process ID invalido: {processIdStr}");
                    return false;
                }

                var process = Process.GetProcessById(processId);
                bool isRunning = !process.HasExited;
                
                if (!isRunning)
                {
                    Logger.LogInfo($"Processo {processId} non più attivo");
                }
                
                return isRunning;
            }
            catch (ArgumentException)
            {
                Logger.LogInfo($"Processo {processIdStr} non trovato nel sistema");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Impossibile verificare stato processo {processIdStr}: {ex.Message}");
                return true;
            }
        }
    }
}
