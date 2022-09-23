namespace Imato.Services.RegularWorker
{
    public class WorkerSettings
    {
        public bool RunOnlyOnPrimaryServer { get; set; } = true;
        public int StartInterval { get; set; } = 5000;
    }
}