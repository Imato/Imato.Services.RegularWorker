using Microsoft.Extensions.Configuration;

namespace Imato.Services.RegularWorker
{
    public static class ConfigurationUtils
    {
        public static string GetConnectionString(this IConfiguration configuration)
        {
            return configuration.GetConnectionString("mssql");
        }
    }
}