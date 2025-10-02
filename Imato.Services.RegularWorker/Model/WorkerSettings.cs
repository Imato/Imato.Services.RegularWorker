using System;

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

        public string[] ExecutionTimes { get; set; } = Array.Empty<string>();

        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Maximum execution milliseconds before restart
        /// </summary>
        public int MaxExecutionTime { get; set; } = 600_000;

        /// <summary>
        /// Restart worker after RestartInterval milliseconds
        /// </summary>
        public int RestartInterval { get; set; }

        public string? Server { get; set; }

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
        /// Each, EveryWhere, Many servers
        /// </summary>
        EveryWhere,

        /// <summary>
        /// Primary, master (DB) server only
        /// </summary>
        PrimaryServer,

        /// <summary>
        /// Secondary, slave (DB) server only
        /// </summary>
        SecondaryServer,

        /// <summary>
        /// First secondary, slave (DB) server only
        /// </summary>
        SecondaryServerFirst,

        /// <summary>
        /// Start on one, first single server
        /// </summary>
        Single
    }
}