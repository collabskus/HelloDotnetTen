using System.Text;
using System.Text.Json;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace HelloDotnetTen.Console.Exporters;

/// <summary>
/// Custom file exporter for OpenTelemetry metrics.
/// Writes metrics to JSON files with daily rotation and size limits.
/// </summary>
public class FileMetricExporter : BaseExporter<Metric>
{
    private readonly FileExporterOptions _options;
    private readonly object _lock = new();
    private StreamWriter? _writer;
    private string _currentFilePath = string.Empty;
    private DateTime _currentFileDate;
    private long _currentFileSize;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileMetricExporter(FileExporterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        EnsureDirectoryExists();
    }

    public override ExportResult Export(in Batch<Metric> batch)
    {
        using var scope = SuppressInstrumentationScope.Begin();
        
        try
        {
            lock (_lock)
            {
                EnsureWriter();
                
                foreach (var metric in batch)
                {
                    foreach (var metricPoint in metric.GetMetricPoints())
                    {
                        var record = SerializeMetricPoint(metric, metricPoint);
                        var json = JsonSerializer.Serialize(record, _jsonOptions);
                        
                        var bytesToWrite = Encoding.UTF8.GetByteCount(json) + Environment.NewLine.Length;
                        if (_currentFileSize + bytesToWrite > _options.MaxFileSizeBytes)
                        {
                            RotateFile();
                        }
                        
                        _writer!.WriteLine(json);
                        _currentFileSize += bytesToWrite;
                    }
                }
                
                _writer!.Flush();
            }
            
            return ExportResult.Success;
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine($"[FileMetricExporter] Export failed: {ex.Message}");
            return ExportResult.Failure;
        }
    }

    private object SerializeMetricPoint(Metric metric, MetricPoint metricPoint)
    {
        var tags = new Dictionary<string, string?>();
        foreach (var tag in metricPoint.Tags)
        {
            tags[tag.Key] = tag.Value?.ToString();
        }

        object? value = metric.MetricType switch
        {
            MetricType.LongSum => metricPoint.GetSumLong(),
            MetricType.DoubleSum => metricPoint.GetSumDouble(),
            MetricType.LongGauge => metricPoint.GetGaugeLastValueLong(),
            MetricType.DoubleGauge => metricPoint.GetGaugeLastValueDouble(),
            MetricType.Histogram => SerializeHistogram(metricPoint),
            MetricType.ExponentialHistogram => SerializeExponentialHistogram(metricPoint),
            _ => null
        };

        return new
        {
            Timestamp = metricPoint.EndTime.ToString("O"),
            Name = metric.Name,
            Description = metric.Description,
            Unit = metric.Unit,
            Type = metric.MetricType.ToString(),
            Tags = tags,
            Value = value,
            StartTime = metricPoint.StartTime.ToString("O"),
            EndTime = metricPoint.EndTime.ToString("O"),
            Resource = ParentProvider?.GetResource()?.Attributes
                .ToDictionary(a => a.Key, a => a.Value?.ToString())
        };
    }

    private object SerializeHistogram(MetricPoint metricPoint)
    {
        var bucketCounts = new List<long>();
        var explicitBounds = new List<double>();
        
        foreach (var histogramBucket in metricPoint.GetHistogramBuckets())
        {
            bucketCounts.Add(histogramBucket.BucketCount);
            if (histogramBucket.ExplicitBound != double.PositiveInfinity)
            {
                explicitBounds.Add(histogramBucket.ExplicitBound);
            }
        }

        return new
        {
            Count = metricPoint.GetHistogramCount(),
            Sum = metricPoint.GetHistogramSum(),
            BucketCounts = bucketCounts,
            ExplicitBounds = explicitBounds,
            Min = metricPoint.TryGetHistogramMinMaxValues(out var min, out _) ? min : (double?)null,
            Max = metricPoint.TryGetHistogramMinMaxValues(out _, out var max) ? max : (double?)null
        };
    }

    private object SerializeExponentialHistogram(MetricPoint metricPoint)
    {
        var data = metricPoint.GetExponentialHistogramData();
        
        return new
        {
            Scale = data.Scale,
            ZeroCount = data.ZeroCount,
            PositiveBuckets = SerializeExponentialBuckets(data.PositiveBuckets),
            // Note: NegativeBuckets may not be available in all versions
            // Check if your OpenTelemetry version supports it
        };
    }

    private object SerializeExponentialBuckets(ExponentialHistogramBuckets buckets)
    {
        var bucketCounts = new List<long>();
        foreach (var count in buckets)
        {
            bucketCounts.Add(count);
        }
        
        return new
        {
            Offset = buckets.Offset,
            BucketCounts = bucketCounts
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
            ? $"metrics_{date:yyyy-MM-dd}.json"
            : $"metrics_{date:yyyy-MM-dd}_{fileNumber:D3}.json";
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
