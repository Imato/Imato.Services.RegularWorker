namespace Imato.Services.RegularWorker.Tests
{
    public class LogWorkerTests : BaseTest
    {
        private readonly LogWorker worker;
        private readonly DbLogger.DbLogger logger;

        public LogWorkerTests()
        {
            worker = GetWorker<LogWorker>();
            logger = GetRequiredService<DbLogger.DbLogger>();
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            logger.Dispose();
        }

        [Test]
        public async Task ExecuteAsync()
        {
            await logger.DeleteAsync();
            await worker.ExecuteAsync(CancellationToken.None);
            await Task.Delay(15_000);
            var logs = await Db?.GetLastLogsAsync();
            Assert.That(logs.Count(), Is.GreaterThan(1));
            Assert.That(logs.Any(x => x.Message == "Execute LogWorker"), Is.True);
        }

        [Test]
        public async Task StartAsync()
        {
            var token = new CancellationToken();
            await logger.DeleteAsync();

            Task.Run(() => worker.StartAsync(token));
            await Task.Delay(15_000);

            Assert.That(worker.Started, Is.True, "Started");
            Assert.That(worker.Status.Active, Is.True, "Active");
            Assert.That(worker.Status.Hosts, Is.EqualTo(1));
            Assert.That(worker.Status.Executed, Is.GreaterThan(worker.Status.Date));
            Assert.That(worker.Status.Date, Is.GreaterThan(DateTime.UtcNow.AddSeconds(-10)));
            Assert.That(worker.Status.Executed, Is.GreaterThan(DateTime.UtcNow.AddSeconds(-10)));

            await worker.StopAsync(token);

            Assert.That(worker.Started, Is.False, "Started");
            Assert.That(worker.Status.Active, Is.False, "Active");

            var logs = (await Db?.GetLastLogsAsync(1000))
                .Where(x => x.Source.Contains("LogWorker"))
                .ToArray();

            Assert.That(logs.Length, Is.GreaterThan(1), "logs");
            Assert.That(logs.Any(x => x.Message == "Worker is active on each server"), Is.True);
            Assert.That(logs.Any(x => x.Message == "Execute worker"), Is.True);
        }
    }
}