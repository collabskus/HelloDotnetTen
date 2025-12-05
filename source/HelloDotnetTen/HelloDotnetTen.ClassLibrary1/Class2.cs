using Microsoft.Extensions.Options;

namespace HelloDotnetTen.ClassLibrary1;

public class Class2 : IClass2
{
    private readonly Class2Options _options;

    public Class2(IOptions<Class2Options> options)
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
