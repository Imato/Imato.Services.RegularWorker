using System;

namespace Imato.Services.RegularWorker
{
    public class WorkerStatus
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Host { get; set; } = "";
        public DateTime Date { get; set; }
        public bool Active { get; set; }
        public string Settings { get; set; } = "";
    }
}