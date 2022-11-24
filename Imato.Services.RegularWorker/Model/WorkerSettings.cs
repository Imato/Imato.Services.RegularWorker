namespace Imato.Services.RegularWorker
{
    public class WorkerSettings
    {
        public RunOn RunOn { get; set; } = RunOn.EveryWhere;
        public int StartInterval { get; set; } = 5000;
        public bool Enabled { get; set; } = true;
    }

    public enum RunOn
    {
        None = 0,
        PrimaryServer = 1,
        SecondaryServer = 2,
        EveryWhere = 3
    }
}