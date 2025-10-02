using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Imato.Dapper.DbContext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Imato.Services.RegularWorker
{
    public class WorkersDbContext : DbContext, IWorkersDbContext
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
            await ExecuteAsync(string.Format(Command("UpdateConfig").Text, GetConfigTable()), config);
        }

        public void CreateWorkersTable(IDbConnection connection)
        {
            if (_workerTableExists) return;
            ExecuteAsync("CreateWorkersTable").Wait();
            _workerTableExists = true;
        }

        public WorkerStatus? GetStatus(WorkerStatus status)
        {
            return QueryAsync<WorkerStatus>("GetStatus", status)
                .Result
                .FirstOrDefault();
        }

        public int GetHostCount(string workerName,
            int statusTimeout)
        {
            return QueryFirstAsync<int>("GetHostCount", new { workerName, statusTimeout })
                .Result;
        }

        public int GetOtherHostCount(string workerName,
            string host,
            int statusTimeout)
        {
            return QueryFirstAsync<int>("GetOtherHostCount",
                    new { workerName, host, statusTimeout })
                .Result;
        }

        public async Task<WorkerStatus> SaveStatusAsync(WorkerStatus status)
        {
            return await QueryFirstAsync<WorkerStatus>("SetStatus", status);
        }

        public async Task<IEnumerable<DbLogEvent>> GetLastLogsAsync(int count = 100)
        {
            return await QueryAsync<DbLogEvent>("GetLastLogs", [count, Configuration?.GetSection("Logging:DbLogger:Options:Table")?.Value ?? ""]);
        }
    }
}