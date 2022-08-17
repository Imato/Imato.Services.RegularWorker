namespace Imato.Services.RegularWorker
{
    public interface IWorker
    {
        Task ExecuteAsync(CancellationToken token);

        Task StartAsync(CancellationToken cancellationToken);

        Task StopAsync(CancellationToken cancellationToken);
    }
}