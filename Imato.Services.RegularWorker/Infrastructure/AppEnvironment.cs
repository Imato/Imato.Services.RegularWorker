using System;

namespace Imato.Services.RegularWorker
{
    public static class AppEnvironment
    {
        public static string? GetVariable(string? name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return Environment.GetEnvironmentVariable(name);
        }
    }
}