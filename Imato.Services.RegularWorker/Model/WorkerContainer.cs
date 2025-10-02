using System.Threading;
using System.Threading.Tasks;

namespace Imato.Services.RegularWorker.Model
{
    internal class WorkerContainer
    {
        public IWorker Worker { get; set; } = null!;
        public Task Task { get; set; } = null!;
        public CancellationTokenSource TokenSource { get; set; } = null!;
    }
}