using Imato.Dapper.DbContext;

namespace Imato.Services.RegularWorker
{
    public class DbLoggerOptions
    {
        public string ConnectionString { get; set; } = "";
        public string Table { get; set; } = "";
        public string Columns { get; set; } = "";
        public EnvironmentVariables? Environment { get; set; }
    }
}