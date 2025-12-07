namespace HelloDotnetTen.Console.Exporters;

/// <summary>
/// Configuration options for file-based OpenTelemetry exporters.
/// </summary>
public class FileExporterOptions
{
    /// <summary>
    /// The directory where telemetry files will be written.
    /// </summary>
    public string Directory { get; set; } = "telemetry";

    /// <summary>
    /// Maximum file size in bytes before rotation occurs.
    /// Default is 25MB (25 * 1024 * 1024 bytes).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 25 * 1024 * 1024; // 25MB

    /// <summary>
    /// Creates options with default values.
    /// </summary>
    public static FileExporterOptions Default => new();

    /// <summary>
    /// Creates options for the specified directory with default 25MB size limit.
    /// </summary>
    public static FileExporterOptions ForDirectory(string directory) => new()
    {
        Directory = directory
    };

    /// <summary>
    /// Creates options for the specified directory and max file size in megabytes.
    /// </summary>
    public static FileExporterOptions Create(string directory, int maxFileSizeMb) => new()
    {
        Directory = directory,
        MaxFileSizeBytes = maxFileSizeMb * 1024L * 1024L
    };
}
