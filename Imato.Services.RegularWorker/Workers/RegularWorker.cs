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
            lock (locker)
            {
                if (!started)
                {
                    Logger.LogInformation("Starting service");
                    started = true;
                }
                else
                {
                    return;
                }
            }

            while (!token.IsCancellationRequested)
            {
                if (!Settings.RunOnlyOnPrimaryServer || Db.IsPrimaryServer())
                {
                    startTime = DateTime.Now;
                    await TryAsync(() => ExecuteAsync(token));
                    var wait = Settings.StartInterval - (int)(DateTime.Now - startTime).TotalMilliseconds;
                    if (wait > 0) await Task.Delay(wait);
                }
                else
                {
                    Logger.LogInformation("Wait activation");
                    await Task.Delay(Settings.StartInterval);
                }
            }
        }
    }
}