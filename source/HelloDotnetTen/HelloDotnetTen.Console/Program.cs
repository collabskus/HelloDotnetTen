using HelloDotnetTen.ClassLibrary1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

// Uptrace configuration
const string uptraceEndpoint = "https://api.uptrace.dev";
const string uptraceDsn = "uptrace-dsn=https://20MWRhNvOdzl6e7VCczHvA@api.uptrace.dev?grpc=4317";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "HelloDotnetTen.Console",
            serviceVersion: "1.0.0"))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("HelloDotnetTen.ClassLibrary1")
            .AddConsoleExporter()
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
            .AddConsoleExporter()
            .AddOtlpExporter((options, readerOptions) =>
            {
                options.Endpoint = new Uri(uptraceEndpoint);
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                options.Headers = uptraceDsn;
                readerOptions.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
            });
    })
    .WithLogging(logging =>
    {
        logging
            .AddConsoleExporter()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(uptraceEndpoint);
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                options.Headers = uptraceDsn;
            });
    });

builder.Services.AddHelloDotnetLibrary(builder.Configuration);

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var c1 = app.Services.GetRequiredService<IClass1>();
var c2 = app.Services.GetRequiredService<IClass2>();

logger.LogInformation("========================================");
logger.LogInformation("Starting HelloDotnetTen Console Application");
logger.LogInformation("========================================");

// Scenario 1: Sequential calls to generate baseline metrics
logger.LogInformation("--- Scenario 1: Sequential Operations ---");
for (int i = 1; i <= 10; i++)
{
    logger.LogInformation("Sequential iteration {Iteration}", i);
    var length1 = c1.GetLengthOfInjectedProperty();
    var length2 = c2.GetLengthOfInjectedProperty();
    logger.LogInformation("Class1: {Length1}, Class2: {Length2}", length1, length2);
    await Task.Delay(100); // Small delay between iterations
}

// Scenario 2: Parallel calls to generate concurrent activity
logger.LogInformation("--- Scenario 2: Parallel Operations ---");
var parallelTasks = new List<Task>();
for (int i = 1; i <= 20; i++)
{
    int iteration = i;
    parallelTasks.Add(Task.Run(() =>
    {
        logger.LogInformation("Parallel iteration {Iteration}", iteration);
        var length1 = c1.GetLengthOfInjectedProperty();
        var length2 = c2.GetLengthOfInjectedProperty();
        logger.LogInformation("Parallel result {Iteration}: Class1={Length1}, Class2={Length2}", 
            iteration, length1, length2);
    }));
}
await Task.WhenAll(parallelTasks);

// Scenario 3: Rapid-fire calls to stress metrics
logger.LogInformation("--- Scenario 3: Rapid-Fire Operations ---");
for (int i = 1; i <= 50; i++)
{
    c1.GetLengthOfInjectedProperty();
    c2.GetLengthOfInjectedProperty();
    if (i % 10 == 0)
    {
        logger.LogInformation("Completed {Count} rapid-fire operations", i);
    }
}

// Scenario 4: Burst pattern - simulate real-world traffic spikes
logger.LogInformation("--- Scenario 4: Burst Pattern ---");
for (int burst = 1; burst <= 3; burst++)
{
    logger.LogWarning("Starting burst {Burst}", burst);
    for (int i = 1; i <= 15; i++)
    {
        c1.GetLengthOfInjectedProperty();
        c2.GetLengthOfInjectedProperty();
    }
    logger.LogWarning("Completed burst {Burst} - 15 operations", burst);
    await Task.Delay(500); // Pause between bursts
}

// Scenario 5: Mixed logging levels
logger.LogInformation("--- Scenario 5: Mixed Logging Levels ---");
logger.LogDebug("Debug: Testing Class1");
c1.GetLengthOfInjectedProperty();
logger.LogInformation("Information: Class1 operation completed");
logger.LogWarning("Warning: High frequency operations detected");
c2.GetLengthOfInjectedProperty();
logger.LogError("Error: Simulated error condition for testing");

// Final statistics
logger.LogInformation("========================================");
logger.LogInformation("Total operations completed:");
logger.LogInformation("  - Sequential: 10 iterations");
logger.LogInformation("  - Parallel: 20 iterations");
logger.LogInformation("  - Rapid-fire: 50 iterations");
logger.LogInformation("  - Burst: 45 iterations (3 bursts x 15)");
logger.LogInformation("  - TOTAL: ~125 operations per class");
logger.LogInformation("========================================");

// Give time for all telemetry to flush
logger.LogInformation("Waiting 10 seconds for telemetry to flush to Uptrace...");
await Task.Delay(10000);

logger.LogInformation("Application completed. Check Uptrace for telemetry data.");
