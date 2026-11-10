using System;

namespace GestioneCespiti.Models
{
    public class AppLock
    {
        public string UserName { get; set; }
        public string HostName { get; set; }
        public DateTime LockTime { get; set; }
        public string ProcessId { get; set; }

        public AppLock()
        {
            UserName = string.Empty;
            HostName = string.Empty;
            LockTime = DateTime.Now;
            ProcessId = string.Empty;
        }
    }
}
