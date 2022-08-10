using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using Dapper;

namespace Imato.Services.RegularWorker
{
    public class DbContext
    {
        private readonly string _connectionString;

        public DbContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString();
        }

        protected SqlConnection Connection => new SqlConnection(_connectionString);

        public bool IsPrimaryServer()
        {
            const string sql = "select @@SERVERNAME";
            using (Connection)
            {
                try
                {
                    Connection.QueryFirst<string>(sql);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public async Task<ConfigValue> GetConfigAsync(string name)
        {
            const string sql = "select Id, Name, Value from dbo.Configs where Name = @name";
            using (Connection)
            {
                return await Connection.QueryFirstOrDefaultAsync<ConfigValue>(sql, new { name })
                    ?? throw new ApplicationException($"Cannot find configuration {name}");
            }
        }

        public async Task UpdateConfigAsync(ConfigValue config)
        {
            const string sql = @"
update dbo.Configs
	set Value = @Value
	where Name = @Name;

if @@ROWCOUNT = 0
	insert into dbo.Configs (Name, Value)
	values (@Name, @Value);";

            using (Connection)
            {
                await Connection.ExecuteAsync(sql, config);
            }
        }
    }
}