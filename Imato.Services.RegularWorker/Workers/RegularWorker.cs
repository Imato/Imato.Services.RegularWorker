using Microsoft.Extensions.Logging;
using System;
using System.Net.NetworkInformation;
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
            if (!Settings.Enabled)
            {
                Logger.LogInformation("Worker is disabled");
                return;
            }

            await TryAsync(async () =>
            {
                if (Start())
                {
                    while (!token.IsCancellationRequested && Settings.Enabled)
                    {
                        var status = GetStatus();
                        if (status.Active)
                        {
                            await ExecuteAsync(token);
                            var wait = Settings.StartInterval
                                - (int)(DateTime.Now - status.Date).TotalMilliseconds;
                            if (wait > 0) await Task.Delay(wait);
                        }
                        else
                        {
                            Logger.LogDebug("Wait activation");
                            await Task.Delay(Settings.StartInterval);
                        }
                    }
                }
            });
        }
    }
}