using Dapper;
using System;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Extensions.Logging;
using Imato.Dapper.DbContext;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Imato.Services.RegularWorker
{
    public class WorkersDbContext : DbContext
    {
        private bool _workerTableExists;
        protected string? ConfigurationTable { get; set; }

        public WorkersDbContext(IConfiguration? configuration = null,
            ILogger<WorkersDbContext>? logger = null,
            WorkersConfiguration? workersConfiguration = null)
            : base(configuration, logger,
                  workersConfiguration?.ConnectionString ?? configuration?.GetValue<string>("Workers:ConnectionString"))
        {
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
                    string.Format(Command("GetConfigAsync").Text, GetConfigTable()),
                    new { name });
                if (config != null) return config;
                return new ConfigValue { Name = name, Value = "" };
            }
        }

        public virtual async Task<T> GetConfigAsync<T>() where T : class
        {
            var name = typeof(T).Name;
            return (await GetConfigAsync(name)).GetRequredValue<T>();
        }

        public virtual async Task UpdateConfigAsync(ConfigValue config)
        {
            await ExecuteAsync(string.Format(Command("UpdateConfigAsync").Text, GetConfigTable()), config);
        }

        public void CreateWorkersTable(IDbConnection connection)
        {
            if (_workerTableExists) return;
            ExecuteAsync("CreateWorkersTable").Wait();
            _workerTableExists = true;
        }

        public WorkerStatus? GetStatus(WorkerStatus status)
        {
            using var connection = Connection();
            return QueryAsync<WorkerStatus>("GetStatus", status)
                .Result
                .FirstOrDefault();
        }

        public int GetHostCount(string workerName,
            int statusTimeout)
        {
            using var connection = Connection();
            return QueryFirstAsync<int>("GetHostCount", new { workerName, statusTimeout })
                .Result;
        }

        public int GetOtherHostCount(string workerName,
            string host,
            int statusTimeout)
        {
            using var connection = Connection();
            return QueryFirstAsync<int>("GetOtherHostCount",
                    new { workerName, host, statusTimeout })
                .Result;
        }

        public async Task<WorkerStatus> SetStatusAsync(WorkerStatus status)
        {
            using var connection = Connection();
            return await QueryFirstAsync<WorkerStatus>("SetStatus", status);
        }

        public async Task<IEnumerable<DbLogEvent>> GetLastLogsAsync(int count = 100)
        {
            return await QueryAsync<DbLogEvent>("GetLastLogs", new object[] { count, Configuration?.GetSection("Logging:DbLogger:Options:Table")?.Value ?? "" });
        }
    }
}