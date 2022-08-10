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
            await base.StartAsync(token);

            while (!token.IsCancellationRequested)
            {
                if (Db.IsPrimaryServer())
                {
                    await TryAsync(async () =>
                    {
                        startTime = DateTime.Now;
                        await ExecuteAsync(token);
                        var wait = StartInterval - (int)(DateTime.Now - startTime).TotalMilliseconds;
                        if (wait > 0) Thread.Sleep(wait);
                    });
                }
            }
        }
    }
}