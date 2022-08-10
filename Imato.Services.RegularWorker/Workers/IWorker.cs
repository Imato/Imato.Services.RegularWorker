namespace Imato.Services.RegularWorker
{
    public interface IWorker
    {
        Task ExecuteAsync(CancellationToken token);
    }
}