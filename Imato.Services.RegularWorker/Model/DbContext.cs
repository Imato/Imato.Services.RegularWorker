using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using Dapper;

namespace Imato.Services.RegularWorker
{
    public class DbContext
    {
        private readonly string _connectionString;
        private string? _configTable = null;

        public DbContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString();
        }

        protected SqlConnection GetConnection() => new SqlConnection(_connectionString);

        public bool IsPrimaryServer()
        {
            const string sql = "select top 1 @@SERVERNAME from sys.tables";
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    if (connection.DataSource != "localhost")
                    {
                        return connection.DataSource == Environment.MachineName;
                    }

                    return connection.QueryFirst<string>(sql) == Environment.MachineName;
                }
                catch
                {
                    return false;
                }
            }
        }

        private string GetConfigTable()
        {
            if (_configTable == null)
            {
                using (var connection = GetConnection())
                {
                    var sql = @"
select top 1 schema_name(t.schema_id) + '.' + t.name
	from sys.tables t
	where name like 'config%'";

                    _configTable = connection.QuerySingleOrDefault<string>(sql)
                        ?? throw new Exception("Cannot find config table in DB");
                }
            }

            return _configTable;
        }

        public async Task<ConfigValue> GetConfigAsync(string name)
        {
            var sql = $"select Id, Name, Value from {GetConfigTable()} where Name = @name";
            using (var connection = GetConnection())
            {
                var config = await connection.QueryFirstOrDefaultAsync<ConfigValue>(sql, new { name });
                if (config != null) return config;
                config = new ConfigValue { Name = name, Value = "" };
                await UpdateConfigAsync(config);
                return config;
            }
        }

        public async Task UpdateConfigAsync(ConfigValue config)
        {
            var sql = @"
update {0}
	set Value = @Value
	where Name = @Name;

if @@ROWCOUNT = 0
	insert into {0} (Name, Value)
	values (@Name, @Value);";
            sql = string.Format(sql, GetConfigTable());

            using (var connection = GetConnection())
            {
                await connection.ExecuteAsync(sql, config);
            }
        }
    }
}