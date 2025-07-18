using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Imato.Services.RegularWorker.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        private static IHostBuilder AddService<T>(this IHostBuilder builder,
            T service) where T : class
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(service);
            });
            return builder;
        }

        private static IHostBuilder AddService<T>(this IHostBuilder builder) where T : class
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<T>();
            });
            return builder;
        }

        public static IHostBuilder ConfigureWorkers(
            this IHostBuilder builder,
            Action<WorkersConfiguration>? configFactory = null)
        {
            var config = new WorkersConfiguration();
            configFactory?.Invoke(config);
            builder.AddService(config);
            Constants.App = config.App;
            Constants.FullAppName = config.FullAppName;
            builder.AddService<WorkersDbContext>();
            builder.AddWorkers();
            return builder;
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
            string[]? workersList = null)
        {
            return app.Services.GetServices<IWorker>()
                .Where(x => x.GetType().Name != "WorkersWatcher"
                    && (workersList == null || workersList.Contains(x.GetType().Name)));
        }

        public static void StartWorkers(this IHost app, string[] workersList = null)
        {
            _watcher = new WorkersWatcher(app, workersList);
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

        public static async Task StartWorkersAsync(this IHost app)
        {
            var workersList = Environment.GetCommandLineArgs()
                .Where(x => x.StartsWith("worker=", StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault()
                ?.Split('=')[1]
                ?.Split(";");

            app.StartWorkers(workersList);
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