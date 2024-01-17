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
using Imato.Dapper.DbContext;

namespace Imato.Services.RegularWorker
{
    public abstract class BaseWorker : IHostedService, IWorker
    {
        protected readonly ILogger Logger;
        protected readonly WorkersDbContext Db;
        protected readonly IConfiguration Configuration;
        private readonly IServiceProvider provider;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public string Name { get; }

        public bool Started { get; private set; }
        public WorkerStatus Status { get; private set; }

        public BaseWorker(IServiceProvider provider)
        {
            this.provider = provider;
            Logger = GetService<ILoggerFactory>().CreateLogger(GetType());
            Db = GetService<WorkersDbContext>();
            Configuration = GetService<IConfiguration>();
            Name = GetType().Name;
            Status = new WorkerStatus(Name);
        }

        protected void LogError(Exception ex)
        {
            try
            {
                Logger?.LogError(ex.ToString());
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
            var settings = Settings;
            Logger?.LogDebug($"Settings: {StringExtensions.Serialize(settings)}");
            Logger?.LogDebug($"Status: {StringExtensions.Serialize(Status)}");

            var result = settings.Enabled;
            if (!result) { return result; }

            result = Db.IsDbActive();
            if (!result)
            {
                Logger?.LogWarning("Connection to DB is not active");
                return result;
            }

            result = settings.RunOn == RunOn.EveryWhere;
            if (result)
            {
                Logger?.LogDebug("Worker is active on each server");
                return result;
            }

            var isPrimaty = Db.IsMasterServer();
            result = settings.RunOn == RunOn.PrimaryServer && isPrimaty;
            if (result)
            {
                Logger?.LogDebug("Worker is active on primary server");
                return result;
            }

            result = settings.RunOn == RunOn.SecondaryServerFirst
                && !isPrimaty
                && (Status.Hosts == 1 || Status.Active);
            if (result)
            {
                Logger?.LogDebug("Worker is active on first secondary server");
                return result;
            }

            result = settings.RunOn == RunOn.SecondaryServer
                && !isPrimaty;
            if (result)
            {
                Logger?.LogDebug("Worker is active on secondary server");
                return result;
            }

            if (Status.Hosts <= 1)
            {
                Logger?.LogDebug("Worker is active on single server");
                return true;
            }

            return false;
        }

        protected WorkerStatus GetStatus()
        {
            _semaphore.Wait();

            try
            {
                // First execute
                if (Status.Date.Year < 2000)
                {
                    UpdateStatus();
                }

                var active = CanStart();
                if (Status.Active != active)
                {
                    Status.Active = active;
                    if (Status.Active)
                    {
                        Logger?.LogInformation("Activate worker");
                    }
                    else
                    {
                        Logger?.LogInformation("Deactivate worker");
                    }
                }
                if (!active)
                {
                    Logger?.LogDebug("Worker is not active");
                }

                UpdateStatus();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Set worker status");
            }

            _semaphore.Release();

            return Status;
        }

        protected void UpdateStatus()
        {
            Status.Settings = !string.IsNullOrEmpty(Status.Settings)
                    ? Status.Settings
                    : JsonSerializer.Serialize(Settings, Constants.JsonOptions);
            Status = Db.SetStatus(Status);
        }

        protected bool Start()
        {
            _semaphore.Wait();
            if (!Started)
            {
                Started = true;
                Logger?.LogInformation("Initialize worker");
            }
            _semaphore.Release();

            return Started;
        }

        public virtual async Task StartAsync(CancellationToken token)
        {
            if (!Settings.Enabled)
            {
                Logger?.LogInformation("Worker is disabled");
                return;
            }

            await TryAsync(async () =>
            {
                if (Start() && GetStatus().Active)
                {
                    await ExecuteAsync(token);
                    Status.Executed = await Db.UpdateExecutedAsync(Status.Id);
                }
            });
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync();
            if (Started)
            {
                Started = false;
                Status.Active = false;
                Logger?.LogInformation("Stop worker");
                Db.SetStatus(Status);
            }
            _semaphore.Release();
        }

        protected int StatusTimeout
        {
            get
            {
                var value = Configuration
                    .GetValue<string>("Workers:StatusTimeout");
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

            var dbSettings = Status.Settings;
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
            return provider.GetRequiredService<T>();
        }

        protected IEnumerable<T> GetServices<T>(Func<T, bool> searchFunc) where T : class
        {
            return provider.GetServices<T>().Where(x => searchFunc(x));
        }

        public virtual Task ExecuteAsync(CancellationToken token)
        {
            Logger?.LogDebug("Execute worker");
            return Task.CompletedTask;
        }

        protected async Task<T> LogDuration<T>(Func<Task<T>> task,
            string taskName = "",
            int maxDuration = 10_000)
        {
            var now = DateTime.UtcNow;
            if (Logger?.IsEnabled(LogLevel.Debug) == true)
            {
                Logger?.LogInformation($"Start {taskName}");
            }
            var result = await task();

            var time = (DateTime.UtcNow - now).TotalMilliseconds;
            if (time > maxDuration)
            {
                Logger?.LogWarning($"{taskName} max duration {maxDuration} exceeded - {time}");
            }

            if (Logger?.IsEnabled(LogLevel.Debug) == true)
            {
                Logger?.LogInformation($"End {taskName}. Duration: {time}");
            }

            return result;
        }

        protected async Task LogDuration(Func<Task> task,
            string taskName = "",
            int maxDuration = 10_000)
        {
            await LogDuration(
                async () =>
                {
                    await task();
                    return Task.FromResult(true);
                },
                taskName, maxDuration);
        }
    }
}