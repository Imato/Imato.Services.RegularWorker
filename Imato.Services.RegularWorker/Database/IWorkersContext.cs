using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Imato.Services.RegularWorker
{
    public interface IWorkersContext
    {
        string GetDbName();

        IDbConnection GetConnection(string dbName = "", string connectionName = "");

        bool IsPrimaryServer();

        bool IsDbActive();

        Task<ConfigValue> GetConfigAsync(string name);

        Task UpdateConfigAsync(ConfigValue config);

        IEnumerable<WorkerStatus> GetStatuses(string workerName);

        int GetHostCount(string workerName);

        WorkerStatus SetStatus(WorkerStatus status);
    }
}