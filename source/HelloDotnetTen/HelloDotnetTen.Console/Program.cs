using HelloDotnetTen.ClassLibrary1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

// Uptrace configuration - Use api.uptrace.dev for HTTP
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
    });

// Logging - the parameter is OpenTelemetryLoggerOptions, not a builder
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService(
            serviceName: "HelloDotnetTen.Console",
            serviceVersion: "1.0.0"));
    options.AddConsoleExporter();
    options.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri(uptraceEndpoint);
        otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        otlpOptions.Headers = uptraceDsn;
    });
});

builder.Services.AddHelloDotnetLibrary(builder.Configuration);

var app = builder.Build();

var c1 = app.Services.GetRequiredService<IClass1>();
var c2 = app.Services.GetRequiredService<IClass2>();

Console.WriteLine($"Class1 length: {c1.GetLengthOfInjectedProperty()}");
Console.WriteLine($"Class2 length: {c2.GetLengthOfInjectedProperty()}");

await Task.Delay(5000);
