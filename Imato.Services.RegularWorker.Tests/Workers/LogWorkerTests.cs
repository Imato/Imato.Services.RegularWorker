namespace Imato.Services.RegularWorker.Tests
{
    public class LogWorkerTests : BaseTest
    {
        private readonly LogWorker worker;

        public LogWorkerTests() : base()
        {
            worker = GetWorker<LogWorker>();
        }

        [Test]
        public async Task ExecuteAsync()
        {
            var logger = GetRequiredService<DbLogger>();
            await logger.DeleteAsync();

            await worker.ExecuteAsync(CancellationToken.None);

            var logs = await Db?.GetLastLogsAsync();
            Assert.IsTrue(logs.Count() > 1);
        }

        [Test]
        public async Task StartAsync()
        {
            var token = new CancellationToken();
            var logger = GetRequiredService<DbLogger>();
            await logger.DeleteAsync();

            Task.Run(() => worker.StartAsync(token));
            await Task.Delay(10_000);

            Assert.IsTrue(worker.Started, "Started");
            Assert.IsTrue(worker.Status.Active, "Active");
            Assert.That(worker.Status.Hosts, Is.EqualTo(1));
            Assert.That(worker.Status.Executed, Is.GreaterThan(worker.Status.Date));
            Assert.That(worker.Status.Date, Is.GreaterThan(DateTime.UtcNow.AddSeconds(-10)));
            Assert.That(worker.Status.Executed, Is.GreaterThan(DateTime.UtcNow.AddSeconds(-10)));

            await worker.StopAsync(token);

            Assert.IsFalse(worker.Started, "Started");
            Assert.IsFalse(worker.Status.Active, "Active");

            var logs = (await Db?.GetLastLogsAsync(1000))
                .Where(x => x.Source.Contains("LogWorker"))
                .ToArray();

            Assert.IsTrue(logs.Length > 1, "logs");
            Assert.IsTrue(logs.Any(x => x.Message == "Worker is active on each server"));
            Assert.IsTrue(logs.Any(x => x.Message == "Execute worker"));
        }
    }
}