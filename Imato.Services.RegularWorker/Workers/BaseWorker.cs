using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Imato.Services.RegularWorker
{
    public class BaseWorker : IHostedService, IWorker
    {
        protected readonly ILogger Logger;
        protected readonly DbContext Db;
        private readonly IServiceProvider provider;

        public BaseWorker(IServiceProvider provider)
        {
            this.provider = provider;
            Logger = GetService<ILoggerFactory>().CreateLogger(GetType());
            Db = GetService<DbContext>();
        }

        protected void LogError(Exception ex)
        {
            Logger.LogError(ex.ToString());
        }

        protected async Task TryAsync(Func<Task> func)
        {
            try
            {
                await func();
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        protected void Try(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            if (Db.IsPrimaryServer())
            {
                Logger.LogInformation("Starting service");
            }
            return Task.CompletedTask;
        }

        public virtual Task StopAsync(CancellationToken cancellationToken)
        {
            if (Db.IsPrimaryServer())
            {
                Logger.LogInformation("Stop service");
            }
            return Task.CompletedTask;
        }

        protected T GetService<T>() where T : class
        {
            return provider.GetService<T>() ?? throw new ArgumentException($"Unknown type {typeof(T).Name}");
        }

        public virtual Task ExecuteAsync(CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}