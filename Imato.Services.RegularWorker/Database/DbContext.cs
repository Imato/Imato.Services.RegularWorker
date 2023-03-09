using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using Dapper;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Imato.Services.RegularWorker
{
    public class DbContext
    {
        private readonly string _connectionString;
        private string _dbName = null!;
        private readonly Dictionary<string, SqlConnection> _pool = new Dictionary<string, SqlConnection>();
        private bool _workerTableExists;

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

        public SqlConnection GetConnection(string dbName = "", string connectionName = "")
        {
            if (!string.IsNullOrEmpty(connectionName)
                && _pool.ContainsKey(connectionName))
            {
                return _pool[connectionName];
            }

            var connection = new SqlConnection(_connectionString);

            if (!string.IsNullOrEmpty(dbName))
            {
                var sb = new SqlConnectionStringBuilder(_connectionString);
                sb.InitialCatalog = dbName;
                connection = new SqlConnection(sb.ConnectionString);
            }

            if (!string.IsNullOrEmpty(connectionName)
                && !_pool.ContainsKey(connectionName))
            {
                _pool.Add(connectionName, connection);
            }

            return connection;
        }

        public bool IsPrimaryServer()
        {
            const string sql = "select top 1 @@SERVERNAME from sys.tables";
            using (var connection = GetConnection("master"))
            {
                connection.Open();
                return connection.QueryFirst<string>(sql)
                    .StartsWith(Environment.MachineName);
            }
        }

        public bool IsDbActive()
        {
            const string sqlStatus = @"
declare @status bit = 0;
select @status = cast(iif(status in (65544, 65536), 1, 0) as bit)
	from sys.sysdatabases
	where name = @name
select @status";
            const string sqlServer = "select @@SERVERNAME";

            using (var connection = GetConnection())
            {
                connection.Open();
                var result = connection.QueryFirst<bool>(
                    sqlStatus,
                    new { name = GetDbName() });
                if (result)
                {
                    return result;
                }

                var serverName = connection.QueryFirst<string>(sqlServer);
                return serverName.Equals(
                    Environment.MachineName,
                    StringComparison.OrdinalIgnoreCase);
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

        private void CreateWorkersTable(SqlConnection connection)
        {
            if (_workerTableExists) return;

            const string sql = @"
if object_id('dbo.Workers') is null
begin
create table dbo.Workers
(id int not null identity(1, 1),
name varchar(255) not null,
host varchar(255) not null,
date datetime not null,
settings varchar(2000) not null,
active bit not null);

alter table dbo.Workers
add constraint Workers__PK primary key (id);
alter table dbo.Workers
add constraint Workers__UK unique (name, host);
end";

            connection.Execute(sql);
            _workerTableExists = true;
        }

        internal IEnumerable<WorkerStatus> GetStatuses(string workerName)
        {
            const string sql = "select * from dbo.Workers where name = @workerName";
            using var connection = GetConnection();
            return connection
                .Query<WorkerStatus>(sql, new { workerName });
        }

        internal int GetHostCount(string workerName)
        {
            const string sql = @"
select count(1)
from dbo.Workers (nolock)
where name = @workerName
and date >= dateadd(minute, -1, getdate())
and host != @host";

            using var connection = GetConnection();
            return connection
                .QueryFirst<int>(sql, new { workerName, host = Environment.MachineName });
        }

        internal WorkerStatus SetStatus(WorkerStatus status)
        {
            const string sql = @"
update dbo.Workers
set date = @date, settings = @settings, active = @active
where id = @id
    or (host = @host and name = @name)
if @@ROWCOUNT = 0
insert into dbo.Workers
(name, host, date, settings, active)
values
(@name, @host, @date, @settings, @active);
select top 1 *
from dbo.Workers
where id = @id
or (host = @host and name = @name);";

            using var connection = GetConnection();
            CreateWorkersTable(connection);
            return connection.QueryFirst<WorkerStatus>(sql, status);
        }
    }
}