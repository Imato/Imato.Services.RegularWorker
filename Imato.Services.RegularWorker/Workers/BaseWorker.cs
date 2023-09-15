using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

namespace Imato.Services.RegularWorker
{
    public abstract class BaseWorker : IHostedService, IWorker
    {
        protected readonly ILogger Logger;
        protected readonly DbContext Db;
        protected readonly IConfiguration Configuration;
        private readonly IServiceProvider provider;
        private WorkerStatus _status;

        public string Name { get; }
        public WorkerStatus Status => _status;

        protected readonly object locker = new object();
        protected bool _started;

        public bool Started => _started;

        public BaseWorker(IServiceProvider provider)
        {
            this.provider = provider;
            Logger = GetService<ILoggerFactory>().CreateLogger(GetType());
            Db = GetService<DbContext>();
            Configuration = GetService<IConfiguration>();
            Name = GetType().Name;
            _status = new WorkerStatus(Name);
            _status = Db.GetStatus(_status) ?? _status;
        }

        protected void LogError(Exception ex)
        {
            try
            {
                Logger.LogError(ex.ToString());
            }
            catch { }
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

        protected bool CanStart()
        {
            var result = Db.IsDbActive();
            if (!result)
            {
                Logger.LogWarning("Connection to DB is not active");
                return result;
            }

            result = Settings.Enabled;
            if (!result) { return result; }

            result = Settings.RunOn == RunOn.EveryWhere;
            if (result)
            {
                Logger.LogInformation("Worker is active on each server");
                return result;
            }

            var isPrimaty = Db.IsMasterServer();
            result = Settings.RunOn == RunOn.PrimaryServer && isPrimaty;
            if (result)
            {
                Logger.LogInformation("Worker is active on primary server");
                return result;
            }

            var hosts = Db.GetOtherHostCount(Name, Status.Host, StatusTimeout + 10000);

            result = Settings.RunOn == RunOn.SecondaryServerFirst
                && !isPrimaty && hosts == 0;
            if (result)
            {
                Logger.LogInformation("Worker is active on first secondary server");
                return result;
            }

            result =
                (Settings.RunOn == RunOn.SecondaryServer
                || Settings.RunOn == RunOn.SecondaryServerFirst)
                && !isPrimaty;
            if (result)
            {
                Logger.LogInformation("Worker is active on secondary server");
                return result;
            }

            if (hosts == 0)
            {
                Logger.LogInformation("Worker is active on single server");
                return true;
            }

            return false;
        }

        protected WorkerStatus GetStatus()
        {
            _status.Date = DateTime.Now;
            var active = CanStart();
            if (_status.Active != active)
            {
                _status.Active = active;
                if (_status.Active)
                {
                    Logger.LogInformation("Activate worker");
                }
                else
                {
                    Logger.LogInformation("Deactivate worker");
                }
            }
            if (!active)
            {
                Logger.LogDebug("Worker is not active");
            }

            _status.Settings = !string.IsNullOrEmpty(_status.Settings)
                ? _status.Settings
                : JsonSerializer.Serialize(Settings, Constants.JsonOptions);
            _status = Db.SetStatus(_status);

            return _status;
        }

        protected bool Start()
        {
            lock (locker)
            {
                if (!_started)
                {
                    _started = true;
                    Logger.LogInformation("Initialize worker");
                }
            }

            return _started;
        }

        public virtual async Task StartAsync(CancellationToken token)
        {
            if (!Settings.Enabled)
            {
                Logger.LogInformation("Worker is disabled");
                return;
            }

            await TryAsync(async () =>
            {
                if (Start() && GetStatus().Active)
                {
                    await ExecuteAsync(token);
                }
            });
        }

        public virtual Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                lock (locker)
                {
                    if (_started)
                    {
                        _started = false;
                        _status.Active = false;
                        Logger.LogInformation("Stop worker");
                    }
                }
            }
            catch { }

            return Task.CompletedTask;
        }

        protected int StatusTimeout
        {
            get
            {
                var value = Configuration
                    .GetValue<string>("Workers.StatusTimeout");
                if (int.TryParse(value, out int result))
                {
                    return result;
                }
                return 60000;
            }
        }

        public T? GetSettings<T>() where T : class
        {
            T? settings = default;

            var workersSettings = Configuration
                .GetSection("Workers")
                .Get<Dictionary<string, T>>();

            if (workersSettings != null && workersSettings.ContainsKey(Name))
            {
                settings = workersSettings[Name];
            }

            var dbSettings = _status.Settings;
            if (!string.IsNullOrEmpty(dbSettings)
                && dbSettings != JsonSerializer.Serialize(settings, Constants.JsonOptions))
            {
                settings = JsonSerializer.Deserialize<T>(dbSettings, Constants.JsonOptions) ?? settings;
            }

            return settings;
        }

        public WorkerSettings Settings => GetSettings<WorkerSettings>() ?? new WorkerSettings();

        protected T GetService<T>() where T : class
        {
            return provider.GetService<T>()
                ?? throw new ArgumentException($"Not registered in DI type {typeof(T).Name}");
        }

        protected IEnumerable<T> GetServices<T>(Func<T, bool> searchFunc) where T : class
        {
            return provider.GetServices<T>().Where(x => searchFunc(x));
        }

        public virtual Task ExecuteAsync(CancellationToken token)
        {
            Logger.LogInformation("Execute worker");
            return Task.CompletedTask;
        }
    }
}