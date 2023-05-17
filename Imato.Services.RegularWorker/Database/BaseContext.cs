using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace Imato.Services.RegularWorker.Database
{
    internal class BaseContext
    {
        protected readonly string? connectionString;
        protected string dbName = null!;
        protected readonly ConcurrentDictionary<string, IDbConnection> pool = new ConcurrentDictionary<string, IDbConnection>();
        protected bool workerTableExists;
        protected string? configurationTable;

        public virtual string Name { get; set; } = "unknown";
        public bool IsActive => connectionString != null;

        protected string? GetConnectionString(IConfiguration configuration)
        {
            return configuration.GetConnectionString(Name);
        }

        public BaseContext(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString(Name);
        }
    }
}