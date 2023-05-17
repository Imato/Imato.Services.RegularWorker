using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using System.Reflection;

namespace Imato.Services.RegularWorker
{
    public class DbLogger : ILogger
    {
        private readonly SqlConnection? connection;
        private readonly string category;
        private static readonly ConcurrentQueue<DbLogEvent> queue = new ConcurrentQueue<DbLogEvent>();

        private readonly string? sqlTable, sqlColumns;

        private string sqlSaveLog => $"insert into {sqlTable} ({sqlColumns})" + @"
values (@date, @user, @level, @source, @message, @server);";

        private string sqlClear => $"delete from {sqlTable} where {DateColumn} <= dateadd(day, -0, gatdate());";

        public DbLogger(string category, IOptions<DbLoggerOptions?> options) : this(category, options?.Value)
        {
        }

        public DbLogger(string category, DbLoggerOptions? options)
        {
            var assembly = Assembly.GetEntryAssembly().GetName().Name;
            category = category.Replace($"{assembly}.", "");
            this.category = $"{assembly}: {category}";
            if (options != null)
            {
                connection = new SqlConnection(options.ConnectionString);
                sqlTable = options.Table;
                sqlColumns = options.Columns;
            }
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public string DateColumn => sqlColumns?.Split(",")[0].Trim() ?? "Date";

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && IsActive;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                var log = new DbLogEvent
                {
                    Date = DateTime.Now,
                    Source = category
                };

                log.Message = exception?.ToString() ?? formatter(state, exception) ?? state?.ToString() ?? "Empty";

                switch (logLevel)
                {
                    case LogLevel.Trace:
                        log.Level = 0;
                        break;

                    case LogLevel.Debug:
                        log.Level = 0;
                        break;

                    case LogLevel.Information:
                        log.Level = 1;
                        break;

                    case LogLevel.Warning:
                        log.Level = 2;
                        break;

                    case LogLevel.Error:
                        log.Level = 3;
                        break;

                    case LogLevel.Critical:
                        log.Level = 3;
                        break;

                    case LogLevel.None:
                        log.Level = 0;
                        break;
                }

                if (log.Level == 3)
                {
                    connection.Execute(sqlSaveLog, log);
                }
                else
                {
                    queue.Enqueue(log);
                }
            }
        }

        public async Task Save()
        {
            if (IsActive)
            {
                try
                {
                    while (queue.TryDequeue(out var log) && log != null)
                    {
                        await connection.ExecuteAsync(sqlSaveLog, log);
                    }
                }
                catch { }
            }
        }

        public async Task Clear()
        {
        }

        private bool IsActive =>
            connection != null && sqlTable != null & sqlColumns != null;
    }

    [ProviderAlias("DbLogger")]
    public class DbLoggerProvider : ILoggerProvider
    {
        public DbLoggerOptions? Options { get; }

        public DbLoggerProvider(IOptions<DbLoggerOptions?> options)
        {
            Options = options?.Value;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new DbLogger(categoryName, Options);
        }

        public void Dispose()
        {
        }
    }
}