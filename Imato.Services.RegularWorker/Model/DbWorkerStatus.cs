using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Imato.Services.RegularWorker
{
    [Table("workers")]
    public class DbWorkerStatus
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; } = null!;
        public string Host { get; set; } = null!;
        public string App { get; set; } = null!;
        public string AppFullName { get; set; } = null!;
        public DateTime Date { get; set; }
        public DateTime Executed { get; set; }
        public bool Active { get; set; }
        public string Settings { get; set; } = null!;
        public int Hosts { get; set; }
    }
}