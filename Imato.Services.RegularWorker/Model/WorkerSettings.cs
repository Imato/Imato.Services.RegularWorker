namespace Imato.Services.RegularWorker
{
    public class WorkerSettings : AppSettings
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

        public override bool Equals(object obj)
        {
            var o = obj as WorkerSettings;
            if (o == null) return false;
            return o.Enabled == Enabled
                && o.RunOn == RunOn
                && o.StartInterval == StartInterval;
        }
    }

    /// <summary>
    /// Start worker on server
    /// </summary>
    public enum RunOn
    {
        /// <summary>
        /// Each, EveryWhere
        /// </summary>
        EveryWhere,

        /// <summary>
        /// Primary, master (DB) server
        /// </summary>
        PrimaryServer,

        /// <summary>
        /// Secondary, slave (DB) server
        /// </summary>
        SecondaryServer,

        /// <summary>
        /// First secondary, slave (DB) server
        /// </summary>
        SecondaryServerFirst
    }
}