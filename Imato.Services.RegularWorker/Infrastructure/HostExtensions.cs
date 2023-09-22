using Imato.Services.RegularWorker.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Imato.Services.RegularWorker
{
    public static class HostExtensions
    {
        private static CancellationTokenSource _startToken = new CancellationTokenSource();
        private static WorkersWatcher _watcher = null!;

        private static IEnumerable<Assembly> GetAssemblies()
        {
            yield return Assembly.GetExecutingAssembly();
            var path = Directory.GetCurrentDirectory();
            foreach (var file in Directory.GetFiles(path, "*.dll"))
            {
                Assembly? assembly;
                try
                {
                    assembly = Assembly.LoadFrom(file);
                }
                catch
                {
                    assembly = null;
                }

                if (assembly != null)
                {
                    yield return assembly;
                }
            }
        }

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

        public static IHostBuilder AddDbContext(this IHostBuilder builder,
            DbContext? context = null)
        {
            if (context != null)
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(context);
                });
            }
            else
            {
                foreach (var assembly in GetAssemblies())
                {
                    var contextType = assembly.GetTypes()
                        .Where(x => x.IsClass
                            && x.IsSubclassOf(typeof(Dapper.DbContext.DbContext)))
                        .FirstOrDefault();
                    if (contextType != null)
                    {
                        builder.ConfigureServices(services =>
                        {
                            services.AddSingleton(typeof(DbContext), contextType);
                        });
                        break;
                    }
                }
            }
            return builder;
        }

        public static IHostBuilder ConfigureWorkers(
            this IHostBuilder builder,
            string appName = "",
            string arguments = "")
        {
            Constants.AppArguments = arguments;
            Constants.AppName = appName;

            builder.AddDbContext();
            builder.ConfigureDbLogger();
            builder.AddWorkers();
            return builder;
        }

        public static IHostBuilder AddWorkers(this IHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IWorker, LogWorker>();

                foreach (var assembly in GetAssemblies())
                {
                    foreach (var worker in assembly.DefinedTypes
                                            .Where(x => x.IsClass
                                                && !x.IsInterface
                                                && !x.IsAbstract
                                                && x.ImplementedInterfaces.Contains(typeof(IWorker))))
                    {
                        if (worker.AsType() != typeof(WorkersWatcher))
                        {
                            services.AddSingleton(typeof(IWorker), worker.AsType());
                        }
                    }
                }
            });

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