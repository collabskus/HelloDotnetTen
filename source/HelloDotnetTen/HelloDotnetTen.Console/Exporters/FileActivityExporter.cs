using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OpenTelemetry;

namespace HelloDotnetTen.Console.Exporters;

/// <summary>
/// Custom file exporter for OpenTelemetry traces (Activities).
/// Writes traces to JSON files with daily rotation and size limits.
/// </summary>
public class FileActivityExporter : BaseExporter<Activity>
{
    private readonly FileExporterOptions _options;
    private readonly object _lock = new();
    private StreamWriter? _writer;
    private string _currentFilePath = string.Empty;
    private DateTime _currentFileDate;
    private long _currentFileSize;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileActivityExporter(FileExporterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        EnsureDirectoryExists();
    }

    public override ExportResult Export(in Batch<Activity> batch)
    {
        using var scope = SuppressInstrumentationScope.Begin();
        
        try
        {
            lock (_lock)
            {
                EnsureWriter();
                
                foreach (var activity in batch)
                {
                    var record = SerializeActivity(activity);
                    var json = JsonSerializer.Serialize(record, _jsonOptions);
                    
                    // Check if we need to rotate before writing
                    var bytesToWrite = Encoding.UTF8.GetByteCount(json) + Environment.NewLine.Length;
                    if (_currentFileSize + bytesToWrite > _options.MaxFileSizeBytes)
                    {
                        RotateFile();
                    }
                    
                    _writer!.WriteLine(json);
                    _currentFileSize += bytesToWrite;
                }
                
                _writer!.Flush();
            }
            
            return ExportResult.Success;
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine($"[FileActivityExporter] Export failed: {ex.Message}");
            return ExportResult.Failure;
        }
    }

    private object SerializeActivity(Activity activity)
    {
        return new
        {
            Timestamp = activity.StartTimeUtc.ToString("O"),
            TraceId = activity.TraceId.ToString(),
            SpanId = activity.SpanId.ToString(),
            ParentSpanId = activity.ParentSpanId.ToString(),
            OperationName = activity.OperationName,
            DisplayName = activity.DisplayName,
            Kind = activity.Kind.ToString(),
            Status = new
            {
                Code = activity.Status.ToString(),
                Description = activity.StatusDescription
            },
            StartTime = activity.StartTimeUtc.ToString("O"),
            EndTime = (activity.StartTimeUtc + activity.Duration).ToString("O"),
            DurationMs = activity.Duration.TotalMilliseconds,
            Tags = activity.TagObjects.ToDictionary(t => t.Key, t => t.Value?.ToString()),
            Events = activity.Events.Select(e => new
            {
                Name = e.Name,
                Timestamp = e.Timestamp.ToString("O"),
                Attributes = e.Tags.ToDictionary(t => t.Key, t => t.Value?.ToString())
            }).ToArray(),
            Links = activity.Links.Select(l => new
            {
                TraceId = l.Context.TraceId.ToString(),
                SpanId = l.Context.SpanId.ToString()
            }).ToArray(),
            Resource = ParentProvider?.GetResource()?.Attributes
                .ToDictionary(a => a.Key, a => a.Value?.ToString())
        };
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_options.Directory))
        {
            Directory.CreateDirectory(_options.Directory);
        }
    }

    private void EnsureWriter()
    {
        var today = DateTime.UtcNow.Date;
        
        if (_writer == null || _currentFileDate != today)
        {
            CloseWriter();
            OpenNewFile(today);
        }
    }

    private void OpenNewFile(DateTime date)
    {
        _currentFileDate = date;
        _currentFilePath = GetFilePath(date, 0);
        
        // Find the next available file number if file exists and is at size limit
        int fileNumber = 0;
        while (File.Exists(_currentFilePath))
        {
            var existingSize = new FileInfo(_currentFilePath).Length;
            if (existingSize < _options.MaxFileSizeBytes)
            {
                // Can append to this file
                _currentFileSize = existingSize;
                break;
            }
            fileNumber++;
            _currentFilePath = GetFilePath(date, fileNumber);
        }
        
        _writer = new StreamWriter(_currentFilePath, append: true, Encoding.UTF8);
        _currentFileSize = File.Exists(_currentFilePath) ? new FileInfo(_currentFilePath).Length : 0;
    }

    private void RotateFile()
    {
        CloseWriter();
        
        // Find next available file number
        int fileNumber = 1;
        string newPath;
        do
        {
            newPath = GetFilePath(_currentFileDate, fileNumber);
            fileNumber++;
        } while (File.Exists(newPath) && new FileInfo(newPath).Length >= _options.MaxFileSizeBytes);
        
        _currentFilePath = newPath;
        _writer = new StreamWriter(_currentFilePath, append: true, Encoding.UTF8);
        _currentFileSize = 0;
    }

    private string GetFilePath(DateTime date, int fileNumber)
    {
        var fileName = fileNumber == 0
            ? $"traces_{date:yyyy-MM-dd}.json"
            : $"traces_{date:yyyy-MM-dd}_{fileNumber:D3}.json";
        return Path.Combine(_options.Directory, fileName);
    }

    private void CloseWriter()
    {
        _writer?.Flush();
        _writer?.Dispose();
        _writer = null;
    }

    protected override bool OnShutdown(int timeoutMilliseconds)
    {
        lock (_lock)
        {
            CloseWriter();
        }
        return true;
    }
}
