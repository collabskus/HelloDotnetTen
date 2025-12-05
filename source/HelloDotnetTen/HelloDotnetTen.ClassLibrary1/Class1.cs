using Microsoft.Extensions.Options;

namespace HelloDotnetTen.ClassLibrary1;

public class Class1 : IClass1
{
    private readonly Class1Options _options;

    // Standard Options Pattern injection.
    // The DI container handles finding the right instance.
    public Class1(IOptions<Class1Options> options)
    {
        _options = options.Value;

        if (string.IsNullOrEmpty(_options.InjectedProperty1))
        {
            throw new ArgumentException("Property cannot be null or empty", nameof(_options.InjectedProperty1));
        }
    }

    public int GetLengthOfInjectedProperty()
    {
        return _options.InjectedProperty1.Length;
    }
}
