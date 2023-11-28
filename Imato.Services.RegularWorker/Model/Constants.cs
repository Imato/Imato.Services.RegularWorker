using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Imato.Services.RegularWorker
{
    public static class Constants
    {
        private static string _appName = "";

        public static JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static string AppArguments = "";

        public static string AppName
        {
            get
            {
                if (string.IsNullOrEmpty(_appName))
                {
                    _appName =
                        AppDomain.CurrentDomain.BaseDirectory
                        + Path.DirectorySeparatorChar
                        + Assembly.GetEntryAssembly().GetName().Name
                        + ":"
                        + Assembly.GetEntryAssembly().GetName().Version.ToString()
                        + (string.IsNullOrEmpty(AppArguments) ? "" : " " + AppArguments);
                }
                return _appName;
            }

            set { _appName = !string.IsNullOrEmpty(value) ? value : _appName; }
        }
    }
}