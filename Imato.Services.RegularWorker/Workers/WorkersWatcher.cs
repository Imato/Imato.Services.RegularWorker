using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Imato.Logger.Extensions;
using Imato.Services.RegularWorker.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Imato.Services.RegularWorker.Workers
{
    internal class WorkersWatcher : RegularWorker
    {
        private readonly IHost _app;
        private readonly Dictionary<string, WorkerContainer> _workers = new();
        private readonly string[]? _workersList;

        public WorkersWatcher(IHost app, string[]? workersList = null)
            : base(app.Services.CreateScope().ServiceProvider)
        {
            _app = app;
            _workersList = workersList;
        }

        public override async Task ExecuteAsync(CancellationToken token)
        {
            await base.ExecuteAsync(token);
            await Logger.LogDuration(() => WorkAsync(token), "WorkAsync", 10_000);
        }

        private async Task WorkAsync(CancellationToken token)
        {
            if (_workers.Count == 0)
            {
                foreach (var worker in _app.GetWorkers(_workersList))
                {
                    Logger.LogDebug(() => $"First start {worker.Name}");
                    await StartWorkerAsync(worker, token);
                    await Task.Delay(123);
                }
            }

            foreach (var w in _workers.Values.Select(x => x.Worker).ToArray())
            {
                await MonitorAsync(w, token);
            }
        }

        private async Task MonitorAsync(IWorker worker, CancellationToken token)
        {
            // Long running workers
            if (worker.Status.Active
                && worker.Status.Date.Year > 2000
                && worker.Settings.MaxExecutionTime > 0)
            {
                var duration = (DateTime.Now - worker.Status.Date.ToLocalTime()).TotalMilliseconds;
                var maxDuration = worker.Settings.MaxExecutionTime > worker.Settings.StartInterval
                    ? worker.Settings.MaxExecutionTime
                    : worker.Settings.StartInterval;
                if (duration > maxDuration + 333)
                {
                    Logger.LogWarning(() => $"Long running worker {worker.Name} {(duration / 1000):N0} seconds");
                    Logger.LogWarning(() => $"Restart {worker.Name}");
                    await StopWorkerAsync(worker, token);
                    await StartWorkerAsync(worker, token);
                }
            }

            // Restart after RestartInterval
            if (worker.Status.Active
                && worker.Status.Started.Year > 2000
                && worker.Settings.RestartInterval > 0
                && (DateTime.Now - worker.Status.Started).TotalMilliseconds > worker.Settings.RestartInterval)
            {
                Logger.LogWarning(() => $"Restart worker {worker.Name} after RestartInterval = {worker.Settings.RestartInterval} ms");
                await StopWorkerAsync(worker, token);
                await StartWorkerAsync(worker, token);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var wc in _workers.Values)
            {
                await wc.Worker.StopAsync(cancellationToken);
            }
            await base.StopAsync(cancellationToken);
        }

        private Task StartWorkerAsync(IWorker worker, CancellationToken token)
        {
            if (!_workers.TryGetValue(worker.Name, out var wc))
            {
                wc = new WorkerContainer
                {
                    Worker = worker,
                    TokenSource = new CancellationTokenSource(),
                    Task = Task.Factory.StartNew(() => worker.StartAsync(wc.TokenSource.Token), token)
                };
                _workers.TryAdd(worker.Name, wc);
            }
            return Task.CompletedTask;
        }

        private async Task StopWorkerAsync(IWorker worker, CancellationToken token)
        {
            if (_workers.Remove(worker.Name, out var wc))
            {
                await wc.Worker.StopAsync(token);
                if (!wc.TokenSource.IsCancellationRequested)
                {
                    wc.TokenSource.Cancel();
                }
                await Task.Delay(300);
                try
                {
                    (wc.Task.AsyncState as Thread)?.Abort();
                    wc.Task?.Dispose();
                }
                catch { }
            }
        }
    }
}