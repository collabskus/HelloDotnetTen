using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace HelloDotnetTen.Console.Exporters;

/// <summary>
/// Extension methods for adding file exporters to OpenTelemetry builders.
/// </summary>
public static class FileExporterExtensions
{
    /// <summary>
    /// Adds a file exporter for traces to the TracerProviderBuilder.
    /// </summary>
    /// <param name="builder">The TracerProviderBuilder to add the exporter to.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <returns>The TracerProviderBuilder for method chaining.</returns>
    public static TracerProviderBuilder AddFileExporter(
        this TracerProviderBuilder builder,
        FileExporterOptions? options = null)
    {
        options ??= FileExporterOptions.Default;
        
        return builder.AddProcessor(
            new BatchActivityExportProcessor(new FileActivityExporter(options)));
    }

    /// <summary>
    /// Adds a file exporter for traces with custom configuration.
    /// </summary>
    public static TracerProviderBuilder AddFileExporter(
        this TracerProviderBuilder builder,
        Action<FileExporterOptions> configure)
    {
        var options = new FileExporterOptions();
        configure(options);
        return builder.AddFileExporter(options);
    }

    /// <summary>
    /// Adds a file exporter for metrics to the MeterProviderBuilder.
    /// </summary>
    /// <param name="builder">The MeterProviderBuilder to add the exporter to.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <returns>The MeterProviderBuilder for method chaining.</returns>
    public static MeterProviderBuilder AddFileExporter(
        this MeterProviderBuilder builder,
        FileExporterOptions? options = null)
    {
        options ??= FileExporterOptions.Default;
        
        return builder.AddReader(
            new PeriodicExportingMetricReader(
                new FileMetricExporter(options),
                exportIntervalMilliseconds: 10000)); // Export every 10 seconds
    }

    /// <summary>
    /// Adds a file exporter for metrics with custom configuration.
    /// </summary>
    public static MeterProviderBuilder AddFileExporter(
        this MeterProviderBuilder builder,
        Action<FileExporterOptions> configure)
    {
        var options = new FileExporterOptions();
        configure(options);
        return builder.AddFileExporter(options);
    }

    /// <summary>
    /// Adds a file exporter for metrics with custom export interval.
    /// </summary>
    public static MeterProviderBuilder AddFileExporter(
        this MeterProviderBuilder builder,
        FileExporterOptions options,
        int exportIntervalMilliseconds)
    {
        return builder.AddReader(
            new PeriodicExportingMetricReader(
                new FileMetricExporter(options),
                exportIntervalMilliseconds: exportIntervalMilliseconds));
    }

    /// <summary>
    /// Adds a file exporter for logs to the OpenTelemetryLoggerOptions.
    /// </summary>
    /// <param name="options">The OpenTelemetryLoggerOptions to add the exporter to.</param>
    /// <param name="exporterOptions">Optional configuration options.</param>
    /// <returns>The OpenTelemetryLoggerOptions for method chaining.</returns>
    public static OpenTelemetryLoggerOptions AddFileExporter(
        this OpenTelemetryLoggerOptions options,
        FileExporterOptions? exporterOptions = null)
    {
        exporterOptions ??= FileExporterOptions.Default;
        
        return options.AddProcessor(
            new BatchLogRecordExportProcessor(new FileLogExporter(exporterOptions)));
    }

    /// <summary>
    /// Adds a file exporter for logs with custom configuration.
    /// </summary>
    public static OpenTelemetryLoggerOptions AddFileExporter(
        this OpenTelemetryLoggerOptions options,
        Action<FileExporterOptions> configure)
    {
        var exporterOptions = new FileExporterOptions();
        configure(exporterOptions);
        return options.AddFileExporter(exporterOptions);
    }
}
