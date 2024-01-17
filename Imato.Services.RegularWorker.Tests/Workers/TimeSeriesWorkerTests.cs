using System.Text.Json;

namespace Imato.Services.RegularWorker.Tests
{
    public class TimeSeriesWorkerTests : BaseTest
    {
        private readonly TestWorker worker;

        private readonly WorkerSettings settings = new WorkerSettings
        {
            StartInterval = 0,
            ExecutionTimes = new string[] { $"{DateTime.Now:HH:mm}", "23:01" }
        };

        public TimeSeriesWorkerTests() : base()
        {
            worker = GetWorker<TestWorker>();
            worker.Status.Settings = JsonSerializer.Serialize(settings,
                Constants.JsonOptions);
        }

        [Test]
        public void IsMyTime()
        {
            var times = new string[]
            {
                "23:55", "09:15"
            };

            var result = worker.IsMyTime(times,
                DateTime.Parse("2023-12-18 09:16:45"),
                DateTime.Parse("2023-12-18 00:12:00"));
            Assert.That(result, Is.True);

            result = worker.IsMyTime(times,
                DateTime.Parse("2023-12-18 09:14:45"),
                DateTime.Parse("2023-12-18 00:12:00"));
            Assert.That(result, Is.False);

            result = worker.IsMyTime(times,
                DateTime.Parse("2023-12-18 23:55:01"),
                DateTime.Parse("2023-12-18 09:16:45"));
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ExecutedAsync()
        {
            var token = new CancellationToken();
            var logger = GetRequiredService<Imato.DbLogger.DbLogger>();

            await logger.DeleteAsync();
            await Db.TruncateAsync<DbWorkerStatus>();

            var logWorker = GetWorker<LogWorker>();
            Task.Run(() => logWorker.StartAsync(token));

            Task.Run(() => worker.StartAsync(token));
            await Task.Delay(1000);

            Assert.IsTrue(worker.Started, "Started");
            Assert.IsTrue(worker.Status.Active, "Active");
            Assert.That(worker.Status.Hosts, Is.EqualTo(1), "Hosts");
            Assert.That(worker.Status.Executed, Is.GreaterThan(DateTime.UtcNow.AddSeconds(-3000)), "Executed");

            await worker.StopAsync(token);

            await Task.Delay(15_000);
            var logs = (await Db?.GetLastLogsAsync(1000))
                .Where(x => x.Source.Contains("TestWorker"))
                .ToArray();

            Assert.IsTrue(logs.Length > 1, "Logs");
            Assert.IsTrue(logs.Any(x => x.Message == "Worker is active on each server"), "Logs");
            Assert.IsTrue(logs.Any(x => x.Message == "Execute worker"), "Logs");
        }

        [Test]
        public async Task NotExecutedAsync()
        {
            await ExecutedAsync();

            var token = new CancellationToken();
            var logger = GetRequiredService<Imato.DbLogger.DbLogger>();
            await logger.DeleteAsync();

            Task.Run(() => worker.StartAsync(token));
            await Task.Delay(1000);

            Assert.IsTrue(worker.Started, "Started");
            Assert.That(worker.Status.Executed, Is.LessThan(DateTime.Now.AddSeconds(-5000)), "Executed");

            await worker.StopAsync(token);

            await Task.Delay(15_000);
            var logs = (await Db?.GetLastLogsAsync(1000))
                .Where(x => x.Source.Contains("TestWorker"))
                .ToArray();

            Assert.That(logs.Length, Is.EqualTo(0), "Logs");
        }
    }
}