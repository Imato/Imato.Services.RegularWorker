using System.Reflection;
using System;

namespace Imato.Services.RegularWorker
{
    public class WorkersConfiguration
    {
        /// <summary>
        /// App name for workers table
        /// </summary>
        public string AppName { get; set; } = AppDomain.CurrentDomain.BaseDirectory
                        + Assembly.GetEntryAssembly().GetName().Name
                        + ":"
                        + Assembly.GetEntryAssembly().GetName().Version;

        /// <summary>
        /// Connection string or its name from configuration file for store workers statuses
        /// </summary>
        public string ConnectionString { get; set; } = "";

        /// <summary>
        /// App name with command line argiments
        /// </summary>
        public string FullAppName => AppName
            + (Environment.GetCommandLineArgs()?.Length > 0 ? " " + string.Join(";", Environment.GetCommandLineArgs()) : "");
    }
}