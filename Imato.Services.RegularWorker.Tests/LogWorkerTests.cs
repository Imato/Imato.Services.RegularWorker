namespace Imato.Services.RegularWorker.Tests
{
    public class LogWorkerTests : BaseTest
    {
        private readonly LogWorker worker;
        private readonly WorkersDbContext db;

        public LogWorkerTests() : base()
        {
            worker = GetWorker<LogWorker>();
            db = GetRequiredService<WorkersDbContext>();
        }

        [Test]
        public async Task ExecuteAsync()
        {
            await worker.ExecuteAsync(CancellationToken.None);
            var logger = GetField(worker, "dbLogger") as DbLogger;
            var logs = await db?.GetLastLogsAsync();
            Assert.IsTrue(logs.Count() > 1);
        }
    }
}