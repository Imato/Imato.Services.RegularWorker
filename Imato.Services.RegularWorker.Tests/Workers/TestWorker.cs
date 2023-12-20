namespace Imato.Services.RegularWorker.Tests
{
    public class TestWorker : TimeSeriesWorker
    {
        public TestWorker(IServiceProvider provider) : base(provider)
        {
        }
    }
}