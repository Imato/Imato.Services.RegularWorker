using Microsoft.Extensions.Hosting;

namespace Imato.Services.RegularWorker
{
    public interface IWorker : IHostedService
    {
        string Name { get; }
        bool Started { get; }
        WorkerStatus Status { get; }
        WorkerSettings Settings { get; }
    }
}