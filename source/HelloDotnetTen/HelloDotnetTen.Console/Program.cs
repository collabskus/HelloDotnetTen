using HelloDotnetTen.ClassLibrary1;
using HelloDotnetTen.Console.Exporters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

// Uptrace configuration (keeping existing OTLP export)
const string uptraceEndpoint = "https://api.uptrace.dev";
const string uptraceDsn = "uptrace-dsn=https://20MWRhNvOdzl6e7VCczHvA@api.uptrace.dev?grpc=4317";

// File exporter configuration
// Uses XDG-style directories by default:
// - Windows: %LOCALAPPDATA%/HelloDotnetTen/telemetry
// - Linux: ~/.local/share/HelloDotnetTen/telemetry
// - macOS: ~/Library/Application Support/HelloDotnetTen/telemetry
// Each run creates new files with timestamp in filename (never appends)
var fileExporterOptions = new FileExporterOptions
{
    MaxFileSizeBytes = 25 * 1024 * 1024 // 25MB
};

Console.WriteLine($"[Telemetry] Writing to: {fileExporterOptions.Directory}");
Console.WriteLine($"[Telemetry] Run ID: {fileExporterOptions.RunId}");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "HelloDotnetTen.Console",
            serviceVersion: "1.0.0"))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("HelloDotnetTen.ClassLibrary1")
            // Console exporter for immediate visibility
            .AddConsoleExporter()
            // File exporter for persistent storage
            .AddFileExporter(fileExporterOptions)
            // OTLP exporter for Uptrace
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(uptraceEndpoint);
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                options.Headers = uptraceDsn;
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("HelloDotnetTen.ClassLibrary1")
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            // Console exporter for immediate visibility
            .AddConsoleExporter()
            // File exporter for persistent storage (exports every 10 seconds)
            .AddFileExporter(fileExporterOptions)
            // OTLP exporter for Uptrace
            .AddOtlpExporter((options, readerOptions) =>
            {
                options.Endpoint = new Uri(uptraceEndpoint);
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                options.Headers = uptraceDsn;
                readerOptions.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
            });
    });

// Configure logging with OpenTelemetry
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService(
            serviceName: "HelloDotnetTen.Console",
            serviceVersion: "1.0.0"));
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
    // Console exporter for immediate visibility
    options.AddConsoleExporter();
    // File exporter for persistent storage
    options.AddFileExporter(fileExporterOptions);
    // OTLP exporter for Uptrace
    options.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri(uptraceEndpoint);
        otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        otlpOptions.Headers = uptraceDsn;
    });
});

builder.Services.AddHelloDotnetLibrary(builder.Configuration);

var app = builder.Build();

// Get logger to demonstrate logging
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Starting HelloDotnetTen Console Application");
logger.LogInformation("========================================");

// Test the services
var c1 = app.Services.GetRequiredService<IClass1>();
var c2 = app.Services.GetRequiredService<IClass2>();

logger.LogInformation("--- Scenario 1: Sequential Operations ---");
for (int i = 1; i <= 3; i++)
{
    logger.LogInformation("Sequential iteration {Iteration}", i);
    Console.WriteLine($"Class1 length: {c1.GetLengthOfInjectedProperty()}");
    Console.WriteLine($"Class2 length: {c2.GetLengthOfInjectedProperty()}");
    await Task.Delay(500);
}

logger.LogInformation("--- Scenario 2: Parallel Operations ---");
var tasks = Enumerable.Range(1, 5).Select(async i =>
{
    logger.LogInformation("Parallel task {TaskId} starting", i);
    await Task.Delay(100 * i);
    var len = c1.GetLengthOfInjectedProperty();
    logger.LogInformation("Parallel task {TaskId} completed with length {Length}", i, len);
    return len;
});

var results = await Task.WhenAll(tasks);
logger.LogInformation("Parallel results: {Results}", string.Join(", ", results));

logger.LogInformation("========================================");
logger.LogInformation("Application completed. Check Uptrace for telemetry data.");
logger.LogInformation("Telemetry files written to: {Directory}", fileExporterOptions.Directory);

// Give time for telemetry to flush before app exits
Console.WriteLine("\nWaiting for telemetry to flush...");
await Task.Delay(5000);

Console.WriteLine($"\nTelemetry files for this run (ID: {fileExporterOptions.RunId}):");
Console.WriteLine($"Directory: {fileExporterOptions.Directory}");
if (Directory.Exists(fileExporterOptions.Directory))
{
    var runFiles = Directory.GetFiles(fileExporterOptions.Directory, $"*_{fileExporterOptions.RunId}*.json");
    foreach (var file in runFiles)
    {
        var info = new FileInfo(file);
        Console.WriteLine($"  - {info.Name} ({info.Length:N0} bytes)");
    }
    
    if (runFiles.Length == 0)
    {
        Console.WriteLine("  (no files created yet - telemetry may still be flushing)");
    }
}
else
{
    Console.WriteLine("  (directory not created yet)");
}
