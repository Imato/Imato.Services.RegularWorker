using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Imato.Services.RegularWorker
{
    public abstract class RegularWorker : BaseWorker
    {
        protected RegularWorker(IServiceProvider provider) : base(provider)
        {
        }

        public override async Task StartAsync(CancellationToken token)
        {
            if (!Settings().Enabled)
            {
                Logger.LogInformation("Worker is disabled");
                return;
            }

            if (Start())
            {
                while (!token.IsCancellationRequested)
                {
                    var status = GetStatus();
                    if (status.Active)
                    {
                        await TryAsync(async () => await ExecuteAsync(token));
                        var wait = Settings().StartInterval
                            - (int)(DateTime.Now - status.Date).TotalMilliseconds;
                        if (wait > 0) await Task.Delay(wait);
                    }
                    else
                    {
                        Logger.LogDebug("Wait activation");
                        await Task.Delay(Settings().StartInterval);
                    }
                }
            }
        }
    }
}