using System;
using System.Threading;
using System.Threading.Tasks;
using Imato.Logger.Extensions;
using Microsoft.Extensions.Logging;

namespace Imato.Services.RegularWorker
{
    public abstract class RegularWorker : BaseWorker
    {
        protected RegularWorker(IServiceProvider provider) : base(provider)
        {
        }

        public override async Task StartAsync(CancellationToken token)
        {
            while (Start() && !token.IsCancellationRequested)
            {
                await TryAsync(async () =>
                {
                    var status = await GetStatusAsync();

                    if (status.Active)
                    {
                        if (status.Executed.AddMilliseconds(Settings.StartInterval) <= DateTime.Now)
                        {
                            status.Executed = DateTime.Now;
                            await ExecuteAsync(token);
                        }
                    }
                    else
                    {
                        Logger?.LogDebug(() => "Wait activation");
                    }

                    await Task.Delay(StatusTimeout / 2);
                });
            }
        }
    }
}