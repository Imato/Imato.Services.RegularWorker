using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Imato.Logger.Extensions;

namespace Imato.Services.RegularWorker.Workers
{
    internal class WorkersWatcher : RegularWorker
    {
        private readonly IHost _app;
        private readonly IDictionary<string, Task> _tasks = new Dictionary<string, Task>();
        private readonly IList<IWorker> _workers = new List<IWorker>();
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

        public async Task WorkAsync(CancellationToken token)
        {
            if (_workers.Count == 0)
            {
                foreach (var worker in _app.GetWorkers(_workersList))
                {
                    _workers.Add(worker);
                }
            }

            foreach (var worker in _workers)
            {
                _tasks.TryGetValue(worker.Name, out var existsTask);
                if (!worker.Started && existsTask == null)
                {
                    Logger.LogDebug(() => $"First start {worker.Name}");
                    StartWorker(worker, token);
                    await Task.Delay(123);
                    continue;
                }

                if (existsTask?.Status == TaskStatus.Faulted)
                {
                    Logger.LogWarning(() => $"Restart {worker.Name} after fail");
                    await worker.StopAsync(token);
                    existsTask.Dispose();
                    _tasks.Remove(worker.Name);
                    StartWorker(worker, token);
                }

                if (worker.Status.Date.AddMilliseconds(StatusTimeout / 2) < DateTime.Now)
                {
                    await worker.UpdateStatusAsync();
                }

                Monitor(worker);
            }
        }

        private void Monitor(IWorker worker)
        {
            if (!_tasks.ContainsKey(worker.Name))
            {
                Logger.LogError(() => $"Worker {worker.Name} is not running!");
            }

            if (worker.Status.Active)
            {
                var duration = (DateTime.Now - worker.Status.Date.ToLocalTime()).TotalMilliseconds;
                if (duration > worker.Settings.StartInterval + StatusTimeout)
                {
                    Logger.LogWarning(() => $"Long running worker {worker.Name} {(duration / 1000):N0} seconds");
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