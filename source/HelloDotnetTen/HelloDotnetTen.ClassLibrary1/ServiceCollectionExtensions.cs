using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration.Binder;

namespace HelloDotnetTen.ClassLibrary1;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the library services and binds configuration automatically.
    /// This encapsulates the library's complexity from the consumer.
    /// </summary>
    public static IServiceCollection AddHelloDotnetLibrary(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Bind Options
        // We look for sections named "Class1" and "Class2" in the root config
        services.Configure<Class1Options>(configuration.GetSection(Class1Options.SectionName).Bind);
        services.Configure<Class2Options>(configuration.GetSection(Class2Options.SectionName).Bind);

        // 2. Register Services
        // We register interfaces, allowing the implementation to change without breaking consumers
        services.AddSingleton<IClass1, Class1>();
        services.AddSingleton<IClass2, Class2>();

        return services;
    }
}
