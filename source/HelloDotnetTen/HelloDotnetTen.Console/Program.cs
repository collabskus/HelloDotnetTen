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
    .AddService("HelloDotnetTen.Console");

// Add OpenTelemetry Tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(resourceBuilder)
            .AddSource("HelloDotnetTen.*") // Capture traces from your app
            .AddConsoleExporter(); // Export to console for demo
    })
    .WithMetrics(metrics =>
    {
        metrics
            .SetResourceBuilder(resourceBuilder)
            .AddRuntimeInstrumentation() // .NET runtime metrics
            .AddProcessInstrumentation() // Process metrics
            .AddConsoleExporter(); // Export to console for demo
    });

// Add OpenTelemetry to Logging
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.SetResourceBuilder(resourceBuilder);
    logging.AddConsoleExporter();
});

// Register your library services
builder.Services.AddHelloDotnetLibrary(builder.Configuration);

var app = builder.Build();

// Resolve and use services
var c1 = app.Services.GetRequiredService<IClass1>();
var c2 = app.Services.GetRequiredService<IClass2>();

Console.WriteLine($"Class1 length: {c1.GetLengthOfInjectedProperty()}");
Console.WriteLine($"Class2 length: {c2.GetLengthOfInjectedProperty()}");
