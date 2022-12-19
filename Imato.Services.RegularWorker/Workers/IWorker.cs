using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace Imato.Services.RegularWorker
{
    public interface IWorker : IHostedService
    {
        Task ExecuteAsync(CancellationToken token);
    }
}