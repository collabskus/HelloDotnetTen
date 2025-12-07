using System.Text;
using System.Text.Json;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace HelloDotnetTen.Console.Exporters;

/// <summary>
/// Custom file exporter for OpenTelemetry logs.
/// Writes logs to JSON files with daily rotation and size limits.
/// </summary>
public class FileLogExporter : BaseExporter<LogRecord>
{
    private readonly FileExporterOptions _options;
    private readonly object _lock = new();
    private StreamWriter? _writer;
    private string _currentFilePath = string.Empty;
    private DateTime _currentFileDate;
    private long _currentFileSize;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileLogExporter(FileExporterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        EnsureDirectoryExists();
    }

    public override ExportResult Export(in Batch<LogRecord> batch)
    {
        using var scope = SuppressInstrumentationScope.Begin();
        
        try
        {
            lock (_lock)
            {
                EnsureWriter();
                
                foreach (var logRecord in batch)
                {
                    var record = SerializeLogRecord(logRecord);
                    var json = JsonSerializer.Serialize(record, _jsonOptions);
                    
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
            System.Console.Error.WriteLine($"[FileLogExporter] Export failed: {ex.Message}");
            return ExportResult.Failure;
        }
    }

    private object SerializeLogRecord(LogRecord logRecord)
    {
        var attributes = new Dictionary<string, object?>();
        
        if (logRecord.Attributes != null)
        {
            foreach (var attr in logRecord.Attributes)
            {
                attributes[attr.Key] = attr.Value;
            }
        }

        // Handle state values for structured logging
        logRecord.ForEachScope(ProcessScope, attributes);

        return new
        {
            Timestamp = logRecord.Timestamp.ToString("O"),
            TraceId = logRecord.TraceId.ToString(),
            SpanId = logRecord.SpanId.ToString(),
            TraceFlags = logRecord.TraceFlags.ToString(),
            CategoryName = logRecord.CategoryName,
            Severity = logRecord.Severity?.ToString(),
            SeverityText = logRecord.SeverityText,
            Body = logRecord.Body,
            FormattedMessage = logRecord.FormattedMessage,
            Attributes = attributes,
            EventId = logRecord.EventId.Id != 0 ? new { logRecord.EventId.Id, logRecord.EventId.Name } : null,
            Exception = logRecord.Exception != null ? new
            {
                Type = logRecord.Exception.GetType().FullName,
                Message = logRecord.Exception.Message,
                StackTrace = logRecord.Exception.StackTrace
            } : null,
            Resource = ParentProvider?.GetResource()?.Attributes
                .ToDictionary(a => a.Key, a => a.Value?.ToString())
        };
    }

    private static readonly Action<LogRecordScope, Dictionary<string, object?>> ProcessScope = 
        (scope, state) =>
        {
            foreach (var item in scope)
            {
                if (!state.ContainsKey(item.Key))
                {
                    state[item.Key] = item.Value;
                }
            }
        };

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
        
        int fileNumber = 0;
        while (File.Exists(_currentFilePath))
        {
            var existingSize = new FileInfo(_currentFilePath).Length;
            if (existingSize < _options.MaxFileSizeBytes)
            {
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
            ? $"logs_{date:yyyy-MM-dd}.json"
            : $"logs_{date:yyyy-MM-dd}_{fileNumber:D3}.json";
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
