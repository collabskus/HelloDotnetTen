using HelloDotnetTen.ClassLibrary1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

// Configure OpenTelemetry Resource (identifies your service)
var resourceBuilder = ResourceBuilder
    .CreateDefault()
    .AddService(
        serviceName: "HelloDotnetTen.Console",
        serviceVersion: "1.0.0");

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(resourceBuilder)
            .AddSource("HelloDotnetTen.ClassLibrary1")
            .AddConsoleExporter() // For debugging
            .AddOtlpExporter(options =>
            {
                // Use base endpoint - the exporter will append /v1/traces automatically
                options.Endpoint = new Uri("https://otlp.uptrace.dev");
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                
                // Add Uptrace DSN header
                options.Headers = "uptrace-dsn=https://20MWRhNvOdzl6e7VCczHvA@api.uptrace.dev?grpc=4317";
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .SetResourceBuilder(resourceBuilder)
            .AddMeter("HelloDotnetTen.ClassLibrary1")
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddConsoleExporter() // For debugging
            .AddOtlpExporter((options, metricReaderOptions) =>
            {
                // Use base endpoint - the exporter will append /v1/metrics automatically
                options.Endpoint = new Uri("https://otlp.uptrace.dev");
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                
                // Add Uptrace DSN header
                options.Headers = "uptrace-dsn=https://20MWRhNvOdzl6e7VCczHvA@api.uptrace.dev?grpc=4317";
                
                // Prefer delta temporality (recommended by Uptrace)
                metricReaderOptions.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
            });
    });

// Add OpenTelemetry to Logging
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.SetResourceBuilder(resourceBuilder);
    logging.AddConsoleExporter(); // For debugging
    logging.AddOtlpExporter(options =>
    {
        // Use base endpoint - the exporter will append /v1/logs automatically
        options.Endpoint = new Uri("https://otlp.uptrace.dev");
        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        
        // Add Uptrace DSN header
        options.Headers = "uptrace-dsn=https://20MWRhNvOdzl6e7VCczHvA@api.uptrace.dev?grpc=4317";
    });
});

// Register your library services
builder.Services.AddHelloDotnetLibrary(builder.Configuration);

var app = builder.Build();

// Resolve and use services
var c1 = app.Services.GetRequiredService<IClass1>();
var c2 = app.Services.GetRequiredService<IClass2>();

Console.WriteLine($"Class1 length: {c1.GetLengthOfInjectedProperty()}");
Console.WriteLine($"Class2 length: {c2.GetLengthOfInjectedProperty()}");

// Give time for telemetry to flush before app exits
await Task.Delay(5000);
