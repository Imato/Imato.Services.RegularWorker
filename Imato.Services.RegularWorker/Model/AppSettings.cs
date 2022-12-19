using System;

namespace Imato.Services.RegularWorker
{
    public class AppSettings
    {
        public string EnvironmentName => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        public bool IsDevelopment => EnvironmentName == "Development";
    }
}