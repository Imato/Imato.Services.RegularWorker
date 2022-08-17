## Imato.Services.RegularWorker

### Create long running services and workers

1. Create new worker
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
```csharp  
// Program.cs
using Imato.Services.RegularWorker;

var appBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IHostedService, Worker>();
    })
```

3. Start all worker
```csharp
// Program.cs
using Imato.Services.RegularWorker;

var app = appBuilder.Build();
app.StartHostedServices();
await app.RunAsync();
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


