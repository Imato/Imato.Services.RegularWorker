using System;

namespace Imato.Services.RegularWorker
{
    public class AppSettings
    {
        public static string EnvironmentName =>
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT")
            ?? "Development";

        public static bool IsDevelopment => EnvironmentName == "Development";
    }
}