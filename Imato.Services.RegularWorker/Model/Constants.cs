using System;
using System.Text.Json;

namespace Imato.Services.RegularWorker
{
    internal static class Constants
    {
        public static string App { get; set; } = null!;
        public static string FullAppName { get; set; } = null!;
        public static DateTime MIN_DATE = DateTime.Parse("1900-01-01");

        public static JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}