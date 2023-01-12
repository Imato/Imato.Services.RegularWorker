using Imato.Services.RegularWorker.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Imato.Services.RegularWorker
{
    public static class HostExtensions
    {
        private static CancellationTokenSource _startToken = new CancellationTokenSource();
        private static WorkersWatcher _watcher = null!;

        public static ILoggingBuilder AddDbLoggerConfig(
            this ILoggingBuilder builder,
            Action<DbLoggerOptions> configure)
        {
            builder.Services.AddSingleton<ILoggerProvider, DbLoggerProvider>();
            builder.Services.Configure(configure);
            return builder;
        }

        public static IHostBuilder ConfigureDbLogger(this IHostBuilder builder)
        {
            builder.ConfigureLogging((context, logging) =>
                logging.AddDbLoggerConfig(options =>
                {
                    context.Configuration
                        .GetSection("Logging")
                        .GetSection("DbLogger")
                        .GetSection("Options")
                        .Bind(options);
                })
            );
            return builder;
        }

        public static IEnumerable<IWorker> GetWorkers(
            this IHost app,
            string? workerName = null)
        {
            var provider = app.Services.CreateScope().ServiceProvider;
            return provider
                .GetServices<IWorker>()
                .Where(x => (workerName == null
                            || x.GetType().Name == workerName
                            || x.GetType().Name == "LogWorker")
                            && x.GetType().Name != "WorkersWatcher");
        }

        public static void StartWorkers(this IHost app, string? workerName = null)
        {
            _watcher = new WorkersWatcher(app, workerName);
            Task.Factory
                .StartNew(() => _watcher.StartAsync(_startToken.Token),
                    _startToken.Token);
        }

        public static void StopWorkers(this IHost app)
        {
            _startToken.Cancel();
            foreach (var worker in GetWorkers(app))
            {
                worker.StopAsync(CancellationToken.None).Wait();
            }
        }

        public static T ResolveService<T>(this IHost app) where T : class
        {
            var scope = app.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<T>();
            if (service != null) return service;
            throw new TypeAccessException($"Unknown service {typeof(T).Name}");
        }
    }
}