## Imato.Services.RegularWorker

### Create long running services and workers

1. Create new worker
 Override ExecuteAsync in RegularWorker

```csharp
// Worker.cs
using Imato.Services.RegularWorker;

public class Worker : RegularWorker
{

    public Worker(IServiceProvider provider) : base(provider)
    {
    }

    // Add same work in ExecuteAsync method
    public override async Task ExecuteAsync(CancellationToken token)
    {
        await SendHelloAsync();
    }
}
```

2. Add worker to DI
Simple. All your workers started automatically.
```csharp  
// Program.cs
using Imato.Services.RegularWorker;

var appBuilder = Host.CreateDefaultBuilder(args);
appBuilder.ConfigureWorkers(args);
```

Or manualy
```csharp  
// Program.cs
using Imato.Services.RegularWorker;

var appBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IHostedService, Worker>();
    })
```

Start one worker as microservise from all your workers pool.
```cmd
>myworker.exe 
```

3. Start workers

```csharp
// Program.cs
using Imato.Services.RegularWorker;

var app = appBuilder.Build();
// All 
app.StartAppAsync();
// Or single worker 
await app.StartAppAsync(");
```


4. Configure worker  
appsettings.Example.json
```json
{
"Workers": {
    "LogWorker": {
      "StartInterval": 15000,
      "RunOn": "EveryWhere",
      "Enabled": true
    }
  }
}
```
RunOn: PrimaryServer, SecondaryServer, SecondaryServerFirst, EveryWhere.

Or configure throw field settings in Workers DB table.

```sql
```

### Using Log table in MS SQL server

Add configuration for DbLogger: ConnectionString, Table and table Columns
```json
// appsettings.json
{
  "ConnectionStrings": {
    "mssql": "Data Source=localhost;Initial Catalog=TestDb;Persist Security Info=True;User ID=test;Password=test;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    },
    "DbLogger": {
      "Options": {
        "ConnectionString": "Data Source=localhost;Initial Catalog=TestDb;Persist Security Info=True;User ID=test;Password=test;",
        "Table": "dbo.Logs",
        "Columns": "[Date], [User], [Level], [Source], [Message], [Server]"
      }
    }
  }
}
```

Add logger in DI config
```csharp
// Program.cs
using Imato.Services.RegularWorker;

var appBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureDbLogger();
    .ConfigureServices(services =>
        {
            services.AddSingleton<IHostedService, LogWorker>();
        });

// Start LogWorker and other workers
var app = appBuilder.Build();
app.StartServices();
await app.RunAsync();
```

or shortly configure workers
```csharp
// Program.cs
using Imato.Services.RegularWorker;

var appBuilder = Host.CreateDefaultBuilder(args);
appBuilder.ConfigureWorkers();
```

### Monitoring
View actual workers status in different apps and hosts
```sql
select id, name, host, appName, date, settings, active from dbo.workers
```

