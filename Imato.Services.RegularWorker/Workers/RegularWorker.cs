using Microsoft.Extensions.Logging;

namespace Imato.Services.RegularWorker
{
    public abstract class RegularWorker : BaseWorker
    {
        private DateTime startTime;

        protected RegularWorker(IServiceProvider provider) : base(provider)
        {
        }

        public override async Task StartAsync(CancellationToken token)
        {
            if (Settings.Enabled)
            {
                lock (locker)
                {
                    if (!started)
                    {
                        Logger.LogInformation("Starting worker");
                        started = true;
                    }
                    else
                    {
                        return;
                    }
                }

                while (!token.IsCancellationRequested)
                {
                    if (CanStart())
                    {
                        startTime = DateTime.Now;
                        await TryAsync(() => ExecuteAsync(token));
                        var wait = Settings.StartInterval - (int)(DateTime.Now - startTime).TotalMilliseconds;
                        if (wait > 0) await Task.Delay(wait);
                    }
                    else
                    {
                        Logger.LogDebug("Wait activation");
                        await Task.Delay(Settings.StartInterval);
                    }
                }
            }
            else
            {
                Logger.LogInformation("Worker disabled");
            }
        }
    }
}