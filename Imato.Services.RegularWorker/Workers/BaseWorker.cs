using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace Imato.Services.RegularWorker
{
    public class BaseWorker : IHostedService, IWorker
    {
        protected readonly ILogger Logger;
        protected readonly DbContext Db;
        protected readonly IConfiguration Configuration;
        public readonly string Name;
        protected WorkerSettings Settings = new WorkerSettings();
        private readonly IServiceProvider provider;
        protected readonly object locker = new object();
        protected bool started;

        public BaseWorker(IServiceProvider provider)
        {
            this.provider = provider;
            Logger = GetService<ILoggerFactory>().CreateLogger(GetType());
            Db = GetService<DbContext>();
            Configuration = GetService<IConfiguration>();
            Name = GetType().Name;
            LoadSettings();
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

        protected bool CanStart()
        {
            var result = !Settings.Enabled || Settings.RunOn == RunOn.None;
            if (result)
                return false;
            result = Settings.RunOn == RunOn.EveryWhere;
            if (result)
                return result;
            var status = Db.IsPrimaryServer();
            result = Settings.RunOn == RunOn.PrimaryServer && status;
            if (result)
                return result;
            result = Settings.RunOn == RunOn.SecondaryServer && !status;
            if (result)
                return result;
            return false;
        }

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            if (Settings.Enabled)
            {
                lock (locker)
                {
                    if (!started)
                    {
                        Logger.LogInformation("Starting worker");
                        started = true;

                        if (CanStart())
                        {
                            return ExecuteAsync(cancellationToken);
                        }
                    }
                }
            }
            else
            {
                Logger.LogInformation("Worker disabled");
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

                Logger.LogInformation("Stop worker");
                started = false;
            }

            return Task.CompletedTask;
        }

        protected void LoadSettings()
        {
            var workersSettings = Configuration
                .GetSection("Workers")
                .Get<Dictionary<string, WorkerSettings>?>();

            if (workersSettings != null && workersSettings.ContainsKey(Name))
            {
                Settings = workersSettings[Name];
            }
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