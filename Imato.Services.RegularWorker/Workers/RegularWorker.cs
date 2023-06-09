using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Imato.Services.RegularWorker
{
    public abstract class RegularWorker : BaseWorker
    {
        protected DateTime LastExecute = DateTime.MinValue;

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
                            if (LastExecute.AddMilliseconds(Settings.StartInterval) <= DateTime.Now)
                            {
                                LastExecute = DateTime.Now;
                                await ExecuteAsync(token);
                            }
                        }
                        else
                        {
                            Logger.LogDebug("Wait activation");
                        }

                        var waitTime = StatusTimeout;
                        var t = (DateTime.Now - LastExecute).TotalMilliseconds;
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