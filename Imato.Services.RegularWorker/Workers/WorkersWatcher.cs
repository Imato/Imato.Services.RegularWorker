using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Imato.Services.RegularWorker.Workers
{
    internal class WorkersWatcher : RegularWorker
    {
        private readonly IHost _app;
        private readonly IDictionary<string, Task> _tasks = new Dictionary<string, Task>();
        private readonly IList<IWorker> _workers = new List<IWorker>();
        private readonly string? _workerName;

        public WorkersWatcher(IHost app, string? workerName = null)
            : base(app.Services.CreateScope().ServiceProvider)
        {
            _app = app;
            _workerName = workerName;
        }

        public override async Task ExecuteAsync(CancellationToken token)
        {
            await base.ExecuteAsync(token);

            if (_workers.Count == 0)
            {
                foreach (var worker in _app.GetWorkers(_workerName))
                {
                    _workers.Add(worker);
                }
            }

            foreach (var worker in _workers)
            {
                if (!worker.Started
                    && !_tasks.ContainsKey(worker.Name))
                {
                    Logger.LogDebug($"First start {worker.Name}");
                    StartWorker(worker, token);
                    continue;
                }

                if (_tasks.ContainsKey(worker.Name))
                {
                    var existsTask = _tasks[worker.Name];
                    if (existsTask.Status == TaskStatus.Faulted)
                    {
                        Logger.LogWarning($"Restart {worker.Name} after fail");
                        await worker.StopAsync(token);
                        existsTask.Dispose();
                        _tasks.Remove(worker.Name);
                        StartWorker(worker, token);
                    }
                }

                Monitor(worker);
            }
        }

        private void Monitor(IWorker worker)
        {
            if (!_tasks.ContainsKey(worker.Name))
            {
                Logger.LogError($"Worker {worker.Name} is not running!");
            }

            if (worker.Status.Active)
            {
                var duration = (DateTime.Now - worker.Status.Date).TotalMilliseconds;
                if (duration > worker.Settings.StartInterval + StatusTimeout)
                {
                    Logger.LogWarning($"Long running worker {worker.Name} {duration / 1000:N0} seconds");
                }
            }
        }

        private void StartWorker(IWorker worker, CancellationToken token)
        {
            var task = Task
                .Factory
                .StartNew(() => worker.StartAsync(token), token);
            _tasks.Add(worker.Name, task);
        }
    }
}