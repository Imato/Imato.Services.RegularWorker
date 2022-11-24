using Microsoft.Extensions.Options;

namespace Imato.Services.RegularWorker
{
    public class LogWorker : RegularWorker
    {
        private readonly DbLogger dbLogger;

        public LogWorker(IOptions<DbLoggerOptions> options, IServiceProvider provider)
            : base(provider)
        {
            dbLogger = new DbLogger("LogWorker", options.Value);
        }

        public override async Task ExecuteAsync(CancellationToken token)
        {
            await base.ExecuteAsync(token);
            await dbLogger.Save();
        }
    }
}