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
            if (Start())
            {
                while (!token.IsCancellationRequested)
                {
                    await TryAsync(async () =>
                    {
                        var status = await GetStatusAsync();

                        // Execute
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

                        // Wait
                        if (status.Active)
                        {
                            var duration = (DateTime.Now - status.Executed).TotalMilliseconds;
                            var waitTime = duration < int.MaxValue
                                ? Settings.StartInterval - (int)duration
                                : 0;
                            if (waitTime > 0)
                            {
                                await Task.Delay(waitTime);
                            }
                        }
                        else
                        {
                            await Task.Delay(StatusTimeout / 2);
                        }
                    });
                }
            }
        }
    }
}