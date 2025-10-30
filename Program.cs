using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Raven.Client.Documents;
using System.Diagnostics;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

var databaseName = builder.Configuration.GetValue<string>("RavenDb:DatabaseName", "MyAppDatabase");
var ravenDbUrl = builder.Configuration.GetValue<string>("RavenDb:Url", "http://localhost:8080");
var disableRavenDb = builder.Configuration.GetValue<bool>("DisableRavenDb", false);

IDocumentStore documentStore = new DocumentStore
{
    Urls = new[] { ravenDbUrl }, 
    Database = databaseName
};

if (!disableRavenDb)
{
    documentStore.Initialize();

    try
    {
        var databaseRecord = documentStore.Maintenance.Server.Send(new GetDatabaseRecordOperation(databaseName));
        if (databaseRecord == null)
        {
            Console.WriteLine($"Database '{databaseName}' not found. Creating it now...");
            documentStore.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(databaseName)));
            Console.WriteLine($"Database '{databaseName}' created successfully.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error checking/creating database. Please ensure RavenDB is running and accessible. Details: {ex.Message}");
    }
}

builder.Services.AddSingleton(documentStore);
builder.Services.AddHostedService<BackgroundTrafficService>();

var app = builder.Build();

// Basically use the same handler for any path to the webserver
app.MapFallback(async ([FromServices] IDocumentStore store, [FromServices] IConfiguration configuration, HttpContext context) =>
{
    var logDirectory = configuration.GetValue<string>("LogDirectory", AppContext.BaseDirectory);
    var logFilePath = Path.Combine(logDirectory, $"{Guid.NewGuid()}.log");
    Directory.CreateDirectory(logDirectory);
    var stopwatch = Stopwatch.StartNew();
    var disableRavenDb = configuration.GetValue<bool>("DisableRavenDb", false);

    try
    {
        var payloadSize = configuration.GetValue<int>("WebService:SizeInBytes", 1024);
        if (!disableRavenDb)
        {
            var randomValue = new string('a', payloadSize);
            var docId = $"request-data/{Guid.NewGuid()}";
            
            using (var session = store.OpenAsyncSession())
            {
                await session.StoreAsync(new MyData { Id = docId, Value = randomValue }, docId);
                await session.SaveChangesAsync();
            }

            string? readValueFromDb;
            using (var session = store.OpenAsyncSession())
            {
                var doc = await session.LoadAsync<MyData>(docId);
                readValueFromDb = doc?.Value;
            }

            if (readValueFromDb != randomValue)
            {
                throw new InvalidOperationException("Mismatch between written and read RavenDB values.");
            }
        }
        
        var filePayload = new string('c', payloadSize);
        await File.WriteAllTextAsync(logFilePath, filePayload);

        var readFilePayload = await File.ReadAllTextAsync(logFilePath);

        if (readFilePayload != filePayload)
        {
            throw new InvalidOperationException("Mismatch between written and read file timestamps.");
        }

        stopwatch.Stop();
        Console.WriteLine($"Successfully completed all operations in {stopwatch.ElapsedMilliseconds}ms for path: {context.Request.Path}");
        
        return Results.Ok("OK");
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        Console.WriteLine($"Error processing request for path: {context.Request.Path}. Duration: {stopwatch.ElapsedMilliseconds}ms. Error: {ex.Message}");
        
        return Results.StatusCode(500);
    }
});

app.Run();

public class BackgroundTrafficService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly int _sizeInBytes;
    private readonly TimeSpan _interval;
    private readonly bool _disableRavenDb;
    private readonly string _logDirectory;

    public BackgroundTrafficService(IServiceProvider services, IConfiguration configuration)
    {
        _services = services;
        
        // The loop will run every 5 seconds with a 1024 byte payload unless configured otherwise.
        _sizeInBytes = configuration.GetValue<int>("BackgroundService:SizeInBytes", 1024);
        var intervalSeconds = configuration.GetValue<int>("BackgroundService:IntervalSeconds", 5);
        _interval = TimeSpan.FromSeconds(intervalSeconds);
        _disableRavenDb = configuration.GetValue<bool>("DisableRavenDb", false);
        _logDirectory = configuration.GetValue<string>("LogDirectory", AppContext.BaseDirectory);
        Directory.CreateDirectory(_logDirectory);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine(
            $"Background Traffic Service is starting with a payload of {_sizeInBytes} bytes every {_interval.TotalSeconds} seconds.");

        while (!stoppingToken.IsCancellationRequested)
        {
            Console.WriteLine($"Running background service iteration at: {DateTimeOffset.Now}");

            try
            {
                using (var scope = _services.CreateScope())
                {
                    var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();
                    
                    var randomValue = new string('a', _sizeInBytes);
 
                    if (!_disableRavenDb)
                    {
                        var docId = $"background-data/{Guid.NewGuid()}";

                        using (var session = store.OpenAsyncSession())
                        {
                            await session.StoreAsync(new MyData { Id = docId, Value = randomValue }, stoppingToken);
                            await session.SaveChangesAsync(stoppingToken);
                        }

                        using (var session = store.OpenAsyncSession())
                        {
                            var savedData = await session.LoadAsync<MyData>(docId, stoppingToken);
                            if (savedData?.Value != randomValue)
                            {
                                throw new InvalidOperationException("Data read from RavenDB does not match data written.");
                            }
                        }
                    }
                    var logFilePath = Path.Combine(_logDirectory, $"{Guid.NewGuid()}.log");
                    await File.WriteAllTextAsync(logFilePath, randomValue, stoppingToken);

                    var fileContent = await File.ReadAllTextAsync(logFilePath, stoppingToken);
                    if (fileContent != randomValue)
                    {
                        throw new InvalidOperationException("Data read from file does not match data written.");
                    }
                }
                 Console.WriteLine("Background service iteration completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during background service iteration: {ex}");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}

public class MyData
{
    public string Id { get; set; }
    public string? Value { get; set; }
}
