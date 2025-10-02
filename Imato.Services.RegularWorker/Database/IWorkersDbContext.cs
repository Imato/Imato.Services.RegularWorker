using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Imato.Dapper.DbContext;

namespace Imato.Services.RegularWorker
{
    public interface IWorkersDbContext : IDbContext
    {
        void CreateWorkersTable(IDbConnection connection);

        Task<ConfigValue> GetConfigAsync(string name);

        Task<T> GetConfigAsync<T>() where T : class;

        int GetHostCount(string workerName, int statusTimeout);

        Task<IEnumerable<DbLogEvent>> GetLastLogsAsync(int count = 100);

        int GetOtherHostCount(string workerName, string host, int statusTimeout);

        WorkerStatus? GetStatus(WorkerStatus status);

        Task<WorkerStatus> SaveStatusAsync(WorkerStatus status);

        Task UpdateConfigAsync(ConfigValue config);
    }
}