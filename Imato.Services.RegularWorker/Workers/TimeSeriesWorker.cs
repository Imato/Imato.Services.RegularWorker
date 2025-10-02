using System;
using System.Threading;
using System.Threading.Tasks;
using Imato.Logger.Extensions;
using Microsoft.Extensions.Logging;

namespace Imato.Services.RegularWorker
{
    public abstract class TimeSeriesWorker : RegularWorker
    {
        protected TimeSeriesWorker(IServiceProvider provider) : base(provider)
        {
        }

        public bool IsMyTime(string[] executionTimes, DateTime now, DateTime prev)
        {
            if (prev > now)
            {
                return false;
            }

            foreach (var et in executionTimes)
            {
                if (DateTime.TryParse(et, out var t))
                {
                    var d = new DateTime(now.Year, now.Month, now.Day,
                        t.Hour, t.Minute, t.Second);
                    if (d > prev.ToLocalTime()
                        && now > d
                        && (now - d).TotalMilliseconds < StatusTimeout * 2)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override async Task StartAsync(CancellationToken token)
        {
            if (Settings.ExecutionTimes == null
                || Settings.ExecutionTimes.Length == 0)
            {
                throw new ApplicationException($"Empty {nameof(Settings.ExecutionTimes)}) in logger settings");
            }

            while (Start() && !token.IsCancellationRequested)
            {
                await TryAsync(async () =>
                {
                    var status = await GetStatusAsync();

                    if (status.Active)
                    {
                        var now = DateTime.Now;
                        if (IsMyTime(Settings.ExecutionTimes!, now, status.Executed))
                        {
                            status.Executed = now;
                            await ExecuteAsync(token);
                        }
                    }
                    else
                    {
                        Logger?.LogDebug(() => "Wait activation");
                    }

                    await Task.Delay(StatusTimeout / 2);
                });
            }
        }
    }
}