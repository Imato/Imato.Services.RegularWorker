using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Imato.Services.RegularWorker
{
    public class LogWorker : RegularWorker
    {
        private readonly DbLogger dbLogger;

        public LogWorker(IOptions<DbLoggerOptions> options, IServiceProvider provider)
            : base(provider)
        {
            dbLogger = new DbLogger(options.Value, "LogWorker");
        }

        public override async Task ExecuteAsync(CancellationToken token)
        {
            await base.ExecuteAsync(token);
            await LogDuration(() => dbLogger.Save(), "dbLogger.Save");
        }
    }
}