using System;
using System.Reflection;

namespace Imato.Services.RegularWorker
{
    public class WorkersConfiguration
    {
        /// <summary>
        /// App for workers table
        /// </summary>
        public string App { get; set; } = Assembly.GetEntryAssembly().GetName().Name;

        /// <summary>
        /// App name with command line argiments
        /// </summary>
        public string FullAppName =>
            AppDomain.CurrentDomain.BaseDirectory
                        + App
                        + ":"
                        + Assembly.GetEntryAssembly().GetName().Version
            + (Environment.GetCommandLineArgs()?.Length > 1 ? " " + string.Join(";", Environment.GetCommandLineArgs()) : "");

        /// <summary>
        /// Connection string or its name from configuration file for store workers statuses
        /// </summary>
        public string? ConnectionString { get; set; }
    }
}