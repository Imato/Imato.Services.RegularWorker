using Microsoft.Extensions.Logging;

namespace Imato.Services.RegularWorker
{
    public abstract class RegularWorker : BaseWorker, IRegularWorker
    {
        private DateTime startTime;

        protected RegularWorker(IServiceProvider provider) : base(provider)
        {
        }

        public virtual int StartInterval => 5000;

        public override async Task StartAsync(CancellationToken token)
        {
            lock (locker)
            {
                if (started)
                {
                    return;
                }

                if (Db.IsPrimaryServer())
                {
                    Logger.LogInformation("Starting service");
                    started = true;
                }
            }

            while (!token.IsCancellationRequested)
            {
                if (Db.IsPrimaryServer())
                {
                    startTime = DateTime.Now;
                    await TryAsync(() => ExecuteAsync(token));
                    var wait = StartInterval - (int)(DateTime.Now - startTime).TotalMilliseconds;
                    if (wait > 0) await Task.Delay(wait);
                }
                else
                {
                    Logger.LogInformation("Wait activation");
                    await Task.Delay(StartInterval);
                }
            }
        }
    }
}