using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Imato.Dapper.DbContext;
using Imato.Logger.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Imato.Services.RegularWorker
{
    public abstract class BaseWorker : IWorker
    {
        protected readonly ILogger Logger;
        protected readonly WorkersDbContext Db;
        protected readonly IConfiguration Configuration;
        private readonly IServiceProvider provider;
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
            Status = new WorkerStatus
            {
                Name = Name
            };
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
                Logger?.LogError(ex, $"{func.Target}.{func.Method.Name}");
                await Task.Delay(Settings.StartInterval > 0 ? Settings.StartInterval : 5000);
            }
        }

        protected bool GetActive()
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

            if (!string.IsNullOrEmpty(settings.Server))
            {
                result = string.Equals(settings.Server, Status.Host, StringComparison.InvariantCultureIgnoreCase);
                if (!result)
                {
                    Logger?.LogDebug(() => $"Worker can start on {settings.Server} only");
                    return result;
                }
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
                if (Status.Started.Year < 2000)
                {
                    Status.Started = DateTime.Now;
                    Status.StatusTimeout = (new int[3] { Settings.StartInterval, Settings.MaxExecutionTime, StatusTimeout }).Max();
                    await SaveStatusAsync();
                }

                var active = GetActive();
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

                await SaveStatusAsync();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, () => "Set worker status");
            }

            return Status;
        }

        public async Task SaveStatusAsync()
        {
            try
            {
                Status.Settings = !string.IsNullOrEmpty(Status.Settings)
                    ? Status.Settings
                    : JsonSerializer.Serialize(Settings, Constants.JsonOptions);
                Status.Date = DateTime.Now;
                Status = await Db.SaveStatusAsync(Status);
            }
            catch (Exception ex)
            {
                Status.Error = ex.ToString();
                Logger?.LogError(ex, "SaveStatusAsync");
            }
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
                if (Start())
                {
                    var status = await GetStatusAsync();
                    if (status.Active)
                    {
                        status.Executed = DateTime.Now;
                        await ExecuteAsync(token);
                    }
                }
            });
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            if (Started)
            {
                Started = false;
                Status.Active = false;
                Logger?.LogInformation(() => "Stop worker");
                Status.Date = DateTime.Now;
                await Db.SaveStatusAsync(Status);
                Status.Date = Constants.MIN_DATE;
                Status.Started = Constants.MIN_DATE;
            }
        }

        protected int StatusTimeout => Configuration.GetValue<int>("Workers:StatusTimeout");

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