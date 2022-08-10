using System.Text.Json;

namespace Imato.Services.RegularWorker
{
    public static class Constants
    {
        public static JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}