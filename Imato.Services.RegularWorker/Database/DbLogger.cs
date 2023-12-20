using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Data;
using Imato.Dapper.DbContext;
using System.Collections.Generic;
using Dapper;

namespace Imato.Services.RegularWorker
{
    public class DbLogger : ILogger
    {
        private readonly IDbConnection connection;
        private readonly string category;
        private static readonly ConcurrentQueue<DbLogEvent> queue = new ConcurrentQueue<DbLogEvent>();

        private readonly string? sqlTable, sqlColumns;

        public DbLogger(IOptions<DbLoggerOptions?> options,
                string category = "")
            : this(options?.Value, category)
        {
        }

        public DbLogger(DbLoggerOptions? options, string category = "")
        {
            var assembly = Assembly.GetEntryAssembly().GetName().Name;
            category = category.Replace($"{assembly}.", "");
            this.category = $"{assembly}: {category}";
            if (options != null && !string.IsNullOrEmpty(options?.ConnectionString))
            {
                var user = AppEnvironment.GetVariable(options?.Environment?.DbUser);
                var password = AppEnvironment.GetVariable(options?.Environment?.DbUserPassword);
                connection = DbContext.Create(options.ConnectionString, "", user, password);
                sqlTable = options.Table;
                sqlColumns = options.Columns;
            }
        }

        public async Task DeleteAsync()
        {
            await connection.ExecuteAsync($"delete from {sqlTable}");
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                var log = new DbLogEvent
                {
                    Source = category
                };

                log.Message = exception?.ToString() ?? formatter(state, exception) ?? state?.ToString() ?? "Empty";

                switch (logLevel)
                {
                    case LogLevel.Trace:

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

                    case LogLevel.Critical:
                        log.Level = 3;
                        break;

                    case LogLevel.None:
                        log.Level = 0;
                        break;
                }

                queue.Enqueue(log);
            }
        }

        public async Task Save()
        {
            if (connection != null && sqlTable != null & sqlColumns != null)
            {
                try
                {
                    var logs = new List<DbLogEvent>(queue.Count);
                    while (queue.TryDequeue(out var log) && log != null)
                    {
                        logs.Add(log);
                    }

                    await connection.BulkInsertAsync(logs, sqlTable, sqlColumns?.Split(","));
                }
                catch { }
            }
        }
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
            return new DbLogger(Options, categoryName);
        }

        public void Dispose()
        {
        }
    }
}