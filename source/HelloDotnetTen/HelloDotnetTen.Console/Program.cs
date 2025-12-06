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
                options.Endpoint = new Uri("https://otlp.uptrace.dev/v1/traces");
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
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
                options.Endpoint = new Uri("https://otlp.uptrace.dev/v1/metrics");
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                options.Headers = "uptrace-dsn=https://20MWRhNvOdzl6e7VCczHvA@api.uptrace.dev?grpc=4317";
                metricReaderOptions.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
            });
    });

// Add OpenTelemetry to Logging
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.SetResourceBuilder(resourceBuilder);
    logging.AddOtlpExporter(options =>
    {
        // HTTP endpoint for logs
        options.Endpoint = new Uri("https://otlp.uptrace.dev/v1/logs");
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

// Give MORE time for telemetry to flush before app exits
await Task.Delay(5000);
