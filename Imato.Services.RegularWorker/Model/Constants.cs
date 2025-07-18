using System.Text.Json;

namespace Imato.Services.RegularWorker
{
    internal static class Constants
    {
        public static string App { get; set; } = null!;
        public static string FullAppName { get; set; } = null!;

        public static JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}