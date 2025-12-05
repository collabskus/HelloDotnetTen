using HelloDotnetTen.ClassLibrary1;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// Bind two different config sections (one per class)
builder.Services.Configure<ClassLibrary1Settings>("Class1", builder.Configuration.GetSection("ClassLibrary1"));
builder.Services.Configure<ClassLibrary1Settings>("Class2", builder.Configuration.GetSection("ClassLibrary2"));

// Register Class1 using named options
builder.Services.AddSingleton<Class1>(sp =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<ClassLibrary1Settings>>();
    // Pull the named instance
    var settings = opts.Get("Class1");
    return new Class1(settings);
});

// Register Class2 using named options
builder.Services.AddSingleton<Class2>(sp =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<ClassLibrary1Settings>>();
    var settings = opts.Get("Class2");
    return new Class2(settings);
});

var app = builder.Build();

var c1 = app.Services.GetRequiredService<Class1>();
var c2 = app.Services.GetRequiredService<Class2>();

Console.WriteLine($"Class1 length: {c1.GetLengthOfInjectedProperty1()}");
Console.WriteLine($"Class2 length: {c2.GetLengthOfInjectedProperty1()}");
