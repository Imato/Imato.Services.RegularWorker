namespace Imato.Services.RegularWorker
{
    public class WorkerSettings
    {
        /// <summary>
        /// Where worker can be started
        /// </summary>
        public RunOn RunOn { get; set; } = RunOn.EveryWhere;

        /// <summary>
        /// Start every StartInterval milliseconds
        /// </summary>
        public int StartInterval { get; set; } = 5000;

        public bool Enabled { get; set; } = true;
    }

    public enum RunOn
    {
        EveryWhere,
        PrimaryServer,
        SecondaryServer
    }
}