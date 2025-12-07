using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace GestioneCespiti.Services
{
    public static class Logger
    {
        private static readonly string _logFile;
        private static readonly object _logLock = new object();

        static Logger()
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string dataFolder = Path.Combine(exePath, "data");
            _logFile = Path.Combine(dataFolder, "application.log");

            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);
        }

        public static void LogInfo(string message)
        {
            Log("INFO", message, null);
        }

        public static void LogWarning(string message)
        {
            Log("WARNING", message, null);
        }

        public static void LogError(string message, Exception? ex = null)
        {
            Log("ERROR", message, ex);
        }

        private static void Log(string level, string message, Exception? ex)
        {
            lock (_logLock)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendFormat("{0:yyyy-MM-dd HH:mm:ss} [{1}] {2}", DateTime.Now, level, message);

                    if (ex != null)
                    {
                        sb.AppendLine()
                          .Append("Exception: ")
                          .Append(ex.GetType().Name)
                          .Append(": ")
                          .Append(ex.Message)
                          .AppendLine()
                          .Append("StackTrace: ")
                          .Append(ex.StackTrace);
                    }

                    sb.AppendLine();
                    File.AppendAllText(_logFile, sb.ToString(), Encoding.UTF8);

                    FileInfo fileInfo = new FileInfo(_logFile);
                    if (fileInfo.Exists && fileInfo.Length > 10 * 1024 * 1024)
                    {
                        RotateLog();
                    }
                }
                catch
                {
                    try
                    {
                        EventLog.WriteEntry("GestioneCespiti",
                            $"[{level}] {message}",
                            level == "ERROR" ? EventLogEntryType.Error :
                            level == "WARNING" ? EventLogEntryType.Warning :
                            EventLogEntryType.Information);
                    }
                    catch { }
                }
            }
        }

        private static void RotateLog()
        {
            try
            {
                string backupFile = _logFile.Replace(".log", $"_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                File.Move(_logFile, backupFile);
            }
            catch { }
        }
    }
}
