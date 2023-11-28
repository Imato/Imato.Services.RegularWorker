using System;
using System.Text.Json;

namespace Imato.Services.RegularWorker
{
    public class ConfigValue
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";

        public T? GetValue<T>() where T : class
        {
            if (string.IsNullOrEmpty(Value))
                return default;

            return JsonSerializer.Deserialize<T>(Value, Constants.JsonOptions);
        }

        public T GetRequredValue<T>() where T : class
        {
            return GetValue<T>()
                ?? throw new ApplicationException("Config value is empty");
        }
    }
}