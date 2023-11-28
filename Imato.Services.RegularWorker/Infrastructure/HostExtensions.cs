using Imato.Services.RegularWorker.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace Imato.Services.RegularWorker
{
    public static class HostExtensions
    {
        private static CancellationTokenSource _startToken = new CancellationTokenSource();
        private static WorkersWatcher _watcher = null!;

        private static IEnumerable<Assembly> GetAssemblies()
        {
            var assembly = Assembly.GetExecutingAssembly();
            yield return assembly;
            var path = Path.GetDirectoryName(assembly.Location);
            foreach (var file in Directory.GetFiles(path, "*.dll"))
            {
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
                    assembly = null;
                }
            }
        }

        public static ILoggingBuilder AddDbLoggerConfig(
            this ILoggingBuilder builder,
            Action<DbLoggerOptions> configure)
        {
            builder.Services.AddSingleton<ILoggerProvider, DbLoggerProvider>();
            builder.Services.Configure(configure);
            SqlMapper.AddTypeMap(typeof(LogLevel), DbType.String);
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
            Type contextType)
        {
            var wt = typeof(WorkersDbContext);

            if (contextType != null
                && contextType.IsSubclassOf(wt))
            {
                builder.ConfigureServices(services =>
                {
                    if (services.Any(x => x.ServiceType == wt))
                    {
                        services.Remove(new ServiceDescriptor(wt,
                            ServiceLifetime.Singleton));
                    }

                    services.AddSingleton(wt, contextType);
                });
            }

            return builder;
        }

        public static IHostBuilder AddDbContext(this IHostBuilder builder)
        {
            var wt = typeof(WorkersDbContext);

            foreach (var assembly in GetAssemblies())
            {
                try
                {
                    if (assembly != null)
                    {
                        var ct = assembly
                            .GetTypes()
                            .Where(x => x.IsClass
                                && !x.IsInterface
                                && !x.IsAbstract
                                && (x == wt))
                            .FirstOrDefault();
                        if (ct != null)
                        {
                            builder.AddDbContext(ct);
                        }

                        ct = assembly
                            .GetTypes()
                            .Where(x => x.IsClass
                                && !x.IsInterface
                                && !x.IsAbstract
                                && (x.IsSubclassOf(wt)))
                            .FirstOrDefault();
                        if (ct != null)
                        {
                            builder.AddDbContext(ct);
                        }
                    }
                }
                catch { }
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

        public static IHostBuilder ConfigureWorkers(
            this IHostBuilder builder,
            string[]? args = null)
        {
            return ConfigureWorkers(builder,
                arguments: string.Join(";", args ?? Array.Empty<string>()));
        }

        public static IHostBuilder AddWorkers(this IHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                foreach (var assembly in GetAssemblies())
                {
                    try
                    {
                        if (assembly != null)
                        {
                            foreach (var worker in
                                assembly.DefinedTypes
                                        .Where(x => x.IsClass
                                            && !x.IsInterface
                                            && !x.IsAbstract
                                            && x.ImplementedInterfaces.Contains(typeof(IWorker)))
                                        .Select(x => x.AsType()))
                            {
                                if (worker != typeof(WorkersWatcher)
                                    && !services.Any(x => x.ImplementationType == worker))
                                {
                                    services.AddSingleton(typeof(IWorker), worker);
                                }
                            }
                        }
                    }
                    catch { }
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

        public static void StartWorkers(this IHost app,
            string[]? args = null)
        {
            var workerName = args
                .Where(x => x.StartsWith("Worker="))
                .FirstOrDefault()
                ?.Split('=')[1];
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

        public static async Task StartAppAsync(this IHost app,
            string[]? args = null)
        {
            app.StartWorkers(args);
            var workers = app.GetWorkers();
            await app.RunAsync()
                .ContinueWith(async (_) =>
                {
                    foreach (var w in workers)
                    {
                        await w.StopAsync(CancellationToken.None);
                    }
                });
        }
    }
}