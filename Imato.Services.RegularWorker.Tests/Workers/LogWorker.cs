using Microsoft.Extensions.Logging;

namespace Imato.Services.RegularWorker.Tests
{
    public class LogWorker : RegularWorker
    {
        public LogWorker(IServiceProvider provider) : base(provider)
        {
        }

        public override Task ExecuteAsync(CancellationToken token)
        {
            Logger.LogInformation("Execute LogWorker");
            return base.ExecuteAsync(token);
        }
    }
}