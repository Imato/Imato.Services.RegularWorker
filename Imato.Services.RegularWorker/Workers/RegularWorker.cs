using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Imato.Logger.Extensions;

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
                        var status = GetStatus();

                        if (status.Active)
                        {
                            if (status.Executed.AddMilliseconds(Settings.StartInterval) <= DateTime.Now)
                            {
                                status.Executed = await Db.UpdateExecutedAsync(Status.Id);
                                await ExecuteAsync(token);
                            }
                        }
                        else
                        {
                            Logger?.LogDebug(() => "Wait activation");
                        }

                        var waitTime = StatusTimeout;
                        var t = (Status.Executed - Status.Date).TotalMilliseconds;
                        if (status.Active)
                        {
                            waitTime = t < int.MaxValue
                                ? Settings.StartInterval - (int)t
                                : 0;
                            waitTime = waitTime < StatusTimeout
                                ? waitTime
                                : StatusTimeout;
                        }

                        if (waitTime > 0)
                        {
                            await Task.Delay(waitTime);
                        }
                    });
                }
            }
        }
    }
}