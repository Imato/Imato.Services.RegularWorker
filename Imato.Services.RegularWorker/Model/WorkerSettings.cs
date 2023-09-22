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

    public enum RunOn
    {
        EveryWhere,
        PrimaryServer,
        SecondaryServer,
        SecondaryServerFirst
    }
}