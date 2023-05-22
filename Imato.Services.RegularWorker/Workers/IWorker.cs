using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace Imato.Services.RegularWorker
{
    public interface IWorker : IHostedService
    {
        string Name { get; }

        Task ExecuteAsync(CancellationToken token);

        bool Started { get; }

        WorkerStatus Status { get; }
    }
}