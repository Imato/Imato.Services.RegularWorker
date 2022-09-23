using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Imato.Services.RegularWorker
{
    public class BaseWorker : IHostedService, IWorker
    {
        protected readonly ILogger Logger;
        protected readonly DbContext Db;
        protected WorkerSettings Settings = new WorkerSettings();
        private readonly IServiceProvider provider;
        protected readonly object locker = new object();
        protected bool started = false;

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
            lock (locker)
            {
                if (!started)
                {
                    Logger.LogInformation("Starting service");
                    started = true;

                    if (!Settings.RunOnlyOnPrimaryServer || Db.IsPrimaryServer())
                    {
                        return ExecuteAsync(cancellationToken);
                    }
                }
            }

            return Task.CompletedTask;
        }

        public virtual Task StopAsync(CancellationToken cancellationToken)
        {
            lock (locker)
            {
                if (!started)
                {
                    return Task.CompletedTask;
                }

                Logger.LogInformation("Stop service");
                started = false;
            }

            return Task.CompletedTask;
        }

        protected T GetService<T>() where T : class
        {
            return provider.GetService<T>() ?? throw new ArgumentException($"Unknown type {typeof(T).Name}");
        }

        public virtual Task ExecuteAsync(CancellationToken token)
        {
            Logger.LogInformation("Execute worker");
            return Task.CompletedTask;
        }
    }
}