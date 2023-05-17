using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Text.Json;

namespace Imato.Services.RegularWorker
{
    public class BaseWorker : IHostedService, IWorker
    {
        protected readonly ILogger Logger;
        protected readonly IWorkersContext Db;
        protected readonly IConfiguration Configuration;
        private readonly IServiceProvider provider;

        protected WorkerSettings Settings = new WorkerSettings();
        private WorkerStatus _status;

        public string Name { get; }

        protected readonly object locker = new object();
        protected bool _started;

        public bool Started => _started;

        public BaseWorker(IServiceProvider provider)
        {
            this.provider = provider;
            Logger = GetService<ILoggerFactory>().CreateLogger(GetType());
            Db = GetService<IWorkersContext>();
            Configuration = GetService<IConfiguration>();
            Name = GetType().Name;
            LoadSettings();
            _status = new WorkerStatus
            {
                Name = Name,
                Host = Environment.MachineName,
                Date = DateTime.Now,
                Settings = JsonSerializer.Serialize(Settings, Constants.JsonOptions)
            };
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

            result = Settings.RunOn == RunOn.EveryWhere;
            if (result)
            {
                Logger.LogInformation("Worker is active on each server");
                return result;
            }

            var isPrimaty = Db.IsPrimaryServer();
            result = Settings.RunOn == RunOn.PrimaryServer && isPrimaty;
            if (result)
            {
                Logger.LogInformation("Worker is active on primary server");
                return result;
            }

            result = Settings.RunOn == RunOn.SecondaryServer && !isPrimaty;
            if (result)
            {
                Logger.LogInformation("Worker is active on secondary server");
                return result;
            }

            result = Db.GetHostCount(Name) == 1;
            if (result)
            {
                Logger.LogInformation("Worker is active on single server");
                return result;
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
                Logger.LogInformation("Worker is not active");
            }
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
                        Settings.Enabled = false;
                        Logger.LogInformation("Stop worker");
                    }
                }
            }
            catch { }

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
            return provider.GetService<T>()
                ?? throw new ArgumentException($"Not registered in DI type {typeof(T).Name}");
        }

        public virtual Task ExecuteAsync(CancellationToken token)
        {
            Logger.LogInformation("Execute worker");
            return Task.CompletedTask;
        }
    }
}