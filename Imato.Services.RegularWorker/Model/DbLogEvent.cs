namespace Imato.Services.RegularWorker
{
    public class DbLogEvent
    {
        public DateTime Date { get; set; } = DateTime.Now;
        public string User { get; set; } = "";
        public byte Level { get; set; } = 1;
        public string Source { get; set; } = "";
        public string Message { get; set; } = "";
        public string Server { get; set; } = Environment.MachineName;
    }
}