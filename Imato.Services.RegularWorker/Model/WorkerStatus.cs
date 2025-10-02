using System;

namespace Imato.Services.RegularWorker
{
    public class WorkerStatus
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Host => Environment.MachineName;
        public string App => Constants.App;
        public string AppFullName => Constants.FullAppName;
        public DateTime Started { get; set; } = Constants.MIN_DATE;
        public DateTime Date { get; set; } = Constants.MIN_DATE;
        public DateTime Executed { get; set; } = Constants.MIN_DATE;
        public bool Active { get; set; }
        public string Settings { get; set; } = "";
        public int Hosts { get; set; }
        public int ActiveHosts { get; set; }
        public string? Error { get; set; }
        public int StatusTimeout { get; set; }
    }
}