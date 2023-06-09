using Microsoft.Extensions.Configuration;
using Dapper;
using System;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Extensions.Logging;
using Imato.Dapper.DbContext;

namespace Imato.Services.RegularWorker
{
    public class DbContext : Dapper.DbContext.DbContext
    {
        private bool _workerTableExists;
        protected string? ConfigurationTable { get; set; }

        public DbContext(IConfiguration configuration,
            ILogger<DbContext> logger)
            : base(configuration, logger)
        {
        }

        protected override void AddCommands()
        {
            base.AddCommands();
            ContextCommands.Add(new ContextCommand
            {
                Name = "GetConfigTable",
                Text = @"
select top 1 schema_name(t.schema_id) + '.' + t.name
	from sys.tables t
	where name like 'config%'"
            });
            ContextCommands.Add(new ContextCommand
            {
                Name = "GetConfigAsync",
                Text = $"select Id, Name, Value from {GetConfigTable()} where Name = @name"
            });
            ContextCommands.Add(new ContextCommand
            {
                Name = "UpdateConfigAsync",
                Text = @"
update {0}
	set Value = @Value
	where Name = @Name;

if @@ROWCOUNT = 0
	insert into {0} (Name, Value)
	values (@Name, @Value);"
            });
            ContextCommands.Add(new ContextCommand
            {
                Name = "CreateWorkersTable",
                Text = @"
if object_id('dbo.Workers') is null
begin
create table dbo.Workers
(id int not null identity(1, 1),
name varchar(255) not null,
host varchar(255) not null,
appName varchar(512) not null default '',
date datetime not null,
settings varchar(2000) not null,
active bit not null);

alter table dbo.Workers
add constraint Workers__PK primary key (id);
alter table dbo.Workers
add constraint Workers__UK unique (name, host, appName);
end"
            });
            ContextCommands.Add(new ContextCommand
            {
                Name = "GetStatus",
                Text = "select top 1 * from dbo.Workers where name = @name and host = @host and appName = @appName"
            });
            ContextCommands.Add(new ContextCommand
            {
                Name = "GetHostCount",
                Text = @"
select count(1)
from dbo.Workers (nolock)
where name = @workerName
and date >= dateadd(millisecond, -@statusTimeout, getdate())"
            });
            ContextCommands.Add(new ContextCommand
            {
                Name = "GetOtherHostCount",
                Text = @"
select count(1)
from dbo.Workers (nolock)
where name = @workerName
and host != @host
and date >= dateadd(millisecond, -@statusTimeout, getdate())
and active = 1"
            });
            ContextCommands.Add(new ContextCommand
            {
                Name = "SetStatus",
                Text = @"
update dbo.Workers
set date = @date, active = @active
where id = @id
    or (host = @host and name = @name and appName = @appName)
if @@ROWCOUNT = 0
insert into dbo.Workers
(name, host, appName, date, settings, active)
values
(@name, @host, @appName, @date, @settings, @active);
select top 1 *
from dbo.Workers
where id = @id
or (host = @host and name = @name and appName = @appName);"
            });
        }

        protected string GetConfigTable()
        {
            if (ConfigurationTable == null)
            {
                using (var connection = Connection())
                {
                    ConfigurationTable = connection.QuerySingleOrDefault<string>(Command("GetConfigTable").Text)
                        ?? throw new Exception("Cannot find config table in DB");
                }
            }

            return ConfigurationTable;
        }

        public virtual async Task<ConfigValue> GetConfigAsync(string name)
        {
            using (var connection = Connection())
            {
                var config = await connection.QueryFirstOrDefaultAsync<ConfigValue>(
                    Command("GetConfigAsync").Text,
                    new { name });
                if (config != null) return config;
                config = new ConfigValue { Name = name, Value = "" };
                await UpdateConfigAsync(config);
                return config;
            }
        }

        public virtual async Task UpdateConfigAsync(ConfigValue config)
        {
            var sql = Command("UpdateConfigAsync").Text;
            sql = string.Format(sql, GetConfigTable());

            using (var connection = Connection())
            {
                await connection.ExecuteAsync(sql, config);
            }
        }

        private void CreateWorkersTable(IDbConnection connection)
        {
            if (_workerTableExists) return;
            connection.Execute(Command("CreateWorkersTable").Text);
            _workerTableExists = true;
        }

        public WorkerStatus? GetStatus(WorkerStatus status)
        {
            using var connection = Connection();
            return connection
                .QueryFirstOrDefault<WorkerStatus>(
                    Command("GetStatus").Text,
                    status);
        }

        public int GetHostCount(string workerName,
            int statusTimeout)
        {
            using var connection = Connection();
            return connection
                .QueryFirst<int>(Command("GetHostCount").Text,
                    new { workerName, statusTimeout });
        }

        public int GetOtherHostCount(string workerName,
            string host,
            int statusTimeout)
        {
            using var connection = Connection();
            return connection
                .QueryFirst<int>(Command("GetOtherHostCount").Text,
                    new { workerName, host, statusTimeout });
        }

        public WorkerStatus SetStatus(WorkerStatus status)
        {
            using var connection = Connection();
            CreateWorkersTable(connection);
            return connection.QueryFirst<WorkerStatus>(
                Command("SetStatus").Text,
                status);
        }
    }
}