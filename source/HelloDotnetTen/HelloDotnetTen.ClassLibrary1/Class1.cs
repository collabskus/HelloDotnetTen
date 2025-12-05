using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HelloDotnetTen.ClassLibrary1;

public class Class1 : IClass1
{
    private readonly Class1Options _options;
    private readonly ILogger<Class1> _logger;

    // Inject both IOptions and ILogger through the constructor
    public Class1(IOptions<Class1Options> options, ILogger<Class1> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrEmpty(_options.InjectedProperty1))
        {
            _logger.LogError("InjectedProperty1 cannot be null or empty");
            throw new ArgumentException("Property cannot be null or empty", nameof(_options.InjectedProperty1));
        }

        _logger.LogInformation("Class1 initialized with InjectedProperty1: {Property}", _options.InjectedProperty1);
    }

    public int GetLengthOfInjectedProperty()
    {
        _logger.LogDebug("Getting length of InjectedProperty1");
        var length = _options.InjectedProperty1.Length;
        _logger.LogInformation("InjectedProperty1 length is {Length}", length);
        return length;
    }
}
