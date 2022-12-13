using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using Dapper;

namespace Imato.Services.RegularWorker
{
    public class DbContext
    {
        private readonly string _connectionString;
        private string _dbName = null!;

        protected string? ConfigurationTable { get; set; }

        public DbContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString();
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new ApplicationException("Cannot find connection string in configuration file");
            }
        }

        public string GetDbName()
        {
            if (_dbName == null)
            {
                var sb = new SqlConnectionStringBuilder(_connectionString);
                _dbName = sb.InitialCatalog;
            }

            return _dbName;
        }

        public SqlConnection GetConnection(string dbName = "")
        {
            var connectionString = _connectionString;

            if (!string.IsNullOrEmpty(dbName))
            {
                var sb = new SqlConnectionStringBuilder(connectionString);
                sb.InitialCatalog = dbName;
                connectionString = sb.ConnectionString;
            }

            return new SqlConnection(connectionString);
        }

        public bool IsPrimaryServer()
        {
            const string sql = "select top 1 @@SERVERNAME from sys.tables";
            using (var connection = GetConnection("master"))
            {
                connection.Open();
                if (connection.DataSource != "localhost")
                {
                    return connection.DataSource == Environment.MachineName;
                }

                return connection.QueryFirst<string>(sql) == Environment.MachineName;
            }
        }

        public bool IsDbActive()
        {
            const string sql = @"
declare @status bit = 0;
select @status = cast(iif(status = 65536, 1, 0) as bit)
	from sys.sysdatabases
	where name = @name
select @status";

            using (var connection = GetConnection("master"))
            {
                connection.Open();
                return connection.QueryFirst<bool>(sql, new { name = GetDbName() });
            }
        }

        protected string GetConfigTable()
        {
            if (ConfigurationTable == null)
            {
                using (var connection = GetConnection())
                {
                    var sql = @"
select top 1 schema_name(t.schema_id) + '.' + t.name
	from sys.tables t
	where name like 'config%'";

                    ConfigurationTable = connection.QuerySingleOrDefault<string>(sql)
                        ?? throw new Exception("Cannot find config table in DB");
                }
            }

            return ConfigurationTable;
        }

        public virtual async Task<ConfigValue> GetConfigAsync(string name)
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

        public virtual async Task UpdateConfigAsync(ConfigValue config)
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