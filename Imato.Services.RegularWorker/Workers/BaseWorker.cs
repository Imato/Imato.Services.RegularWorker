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
using Imato.Logger.Extensions;

namespace Imato.Services.RegularWorker
{
    public abstract class BaseWorker : IHostedService, IWorker
    {
        protected readonly ILogger Logger;
        protected readonly WorkersDbContext Db;
        protected readonly IConfiguration Configuration;
        private readonly IServiceProvider provider;
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
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
                Logger?.LogError(() => ex.ToString());
            }
            catch { }
        }

        protected async Task TryAsync(Func<Task> func)
        {
            try
            {
                await func();
                Status.Error = string.Empty;
            }
            catch (Exception ex)
            {
                Status.Error = ex.ToString();
                LogError(ex);
                await Task.Delay(Settings.StartInterval > 0 ? Settings.StartInterval : 5000);
            }
        }

        protected bool CanStart()
        {
            var settings = Settings;
            Logger?.LogDebug(() => $"Settings: {StringExtensions.Serialize(settings)}");
            Logger?.LogDebug(() => $"Status: {StringExtensions.Serialize(Status)}");

            var result = settings.Enabled;
            if (!result)
            {
                Logger?.LogDebug(() => "Worker is disabled");
                return result;
            }

            result = Db.IsDbActive();
            if (!result)
            {
                Logger?.LogWarning(() => "Connection to DB is not active");
                return result;
            }

            result = settings.RunOn == RunOn.EveryWhere;
            if (result)
            {
                Logger?.LogDebug(() => "Worker is active on each server");
                return result;
            }

            var isPrimaty = Db.IsMasterServer();
            result = settings.RunOn == RunOn.PrimaryServer && isPrimaty;
            if (result)
            {
                Logger?.LogDebug(() => "Worker is active on primary server");
                return result;
            }

            if (Status.ActiveHosts == 0 && isPrimaty && settings.RunOn != RunOn.SecondaryServer)
            {
                result = Status.Hosts == 1;
                if (result)
                {
                    Logger?.LogDebug(() => "Worker is active on first master server");
                    return result;
                }
            }

            result = settings.RunOn == RunOn.Single && Status.ActiveHosts == 0;
            if (result)
            {
                Logger?.LogDebug(() => "Worker is active on single server");
                return result;
            }

            result = (settings.RunOn == RunOn.SecondaryServer || settings.RunOn == RunOn.SecondaryServerFirst)
                && Status.ActiveHosts == 0
                && !isPrimaty;
            if (result)
            {
                Logger?.LogDebug(() => "Worker is active on secondary server");
                return result;
            }

            return false;
        }

        protected async Task<WorkerStatus> GetStatusAsync()
        {
            try
            {
                // First execute
                if (Status.Date.Year < 2000)
                {
                    await UpdateStatusAsync();
                }

                var active = CanStart();
                if (Status.Active != active)
                {
                    Status.Active = active;
                    if (Status.Active)
                    {
                        Logger?.LogInformation(() => "Activate worker");
                    }
                    else
                    {
                        Logger?.LogInformation(() => "Deactivate worker");
                    }
                }
                if (!active)
                {
                    Logger?.LogDebug(() => "Worker is not active");
                }

                await UpdateStatusAsync();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, () => "Set worker status");
            }

            return Status;
        }

        public async Task UpdateStatusAsync()
        {
            semaphore.Wait();
            try
            {
                Status.Settings = !string.IsNullOrEmpty(Status.Settings)
                    ? Status.Settings
                    : JsonSerializer.Serialize(Settings, Constants.JsonOptions);
                Status.Date = DateTime.Now;
                Status = await Db.SetStatusAsync(Status);
            }
            catch (Exception ex)
            {
                Status.Error = ex.ToString();
                LogError(ex);
            }
            semaphore.Release();
        }

        protected bool Start()
        {
            if (!Started)
            {
                Started = true;
                Logger?.LogInformation(() => "Initialize worker");
            }

            return Started;
        }

        public virtual async Task StartAsync(CancellationToken token)
        {
            if (!Settings.Enabled)
            {
                Logger?.LogInformation(() => "Worker is disabled");
                return;
            }

            await TryAsync(async () =>
            {
                if (Start() && (await GetStatusAsync()).Active)
                {
                    Status.Executed = DateTime.Now;
                    await ExecuteAsync(token);
                }
            });
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            if (Started)
            {
                Started = false;
                Status.Active = false;
                Status.Date = DateTime.Now;
                Logger?.LogInformation(() => "Stop worker");
                await Db.SetStatusAsync(Status);
            }
        }

        protected int StatusTimeout
        {
            get
            {
                var value = Configuration
                    .GetValue<string>("Workers:StatusTimeout");
                if (value != null && int.TryParse(value, out int result))
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
            Logger?.LogDebug(() => "Execute worker");
            return Task.CompletedTask;
        }
    }
}