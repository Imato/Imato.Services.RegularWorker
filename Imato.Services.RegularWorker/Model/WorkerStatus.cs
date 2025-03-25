using System;

namespace Imato.Services.RegularWorker
{
    public class WorkerStatus
    {
        public WorkerStatus()
        { }

        public WorkerStatus(string name)
        {
            Name = name;
        }

        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Host => Environment.MachineName;
        public string AppName => Constants.FullAppName;
        public DateTime Date { get; set; } = new DateTime(1970, 1, 1).ToLocalTime();
        public DateTime Executed { get; set; } = new DateTime(1970, 1, 1).ToLocalTime();
        public bool Active { get; set; }
        public string Settings { get; set; } = "";
        public int Hosts { get; set; }
        public int ActiveHosts { get; set; }
        public string? Error { get; set; }
    }
}