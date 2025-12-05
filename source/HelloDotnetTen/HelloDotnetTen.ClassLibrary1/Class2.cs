using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HelloDotnetTen.ClassLibrary1;

public class Class2 : IClass2
{
    private static readonly ActivitySource ActivitySource = new("HelloDotnetTen.ClassLibrary1");
    
    private readonly Class2Options _options;
    private readonly ILogger<Class2> _logger;

    public Class2(IOptions<Class2Options> options, ILogger<Class2> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrEmpty(_options.InjectedProperty1))
        {
            _logger.LogError("InjectedProperty1 cannot be null or empty");
            throw new ArgumentException("Property cannot be null or empty", nameof(_options.InjectedProperty1));
        }

        _logger.LogInformation("Class2 initialized with InjectedProperty1: {Property}", _options.InjectedProperty1);
    }

    public int GetLengthOfInjectedProperty()
    {
        using var activity = ActivitySource.StartActivity("GetLengthOfInjectedProperty");
        
        _logger.LogDebug("Getting length of InjectedProperty1");
        var length = _options.InjectedProperty1.Length;
        
        activity?.SetTag("property.length", length);
        activity?.SetTag("property.value", _options.InjectedProperty1);
        
        _logger.LogInformation("InjectedProperty1 length is {Length}", length);
        return length;
    }
}
