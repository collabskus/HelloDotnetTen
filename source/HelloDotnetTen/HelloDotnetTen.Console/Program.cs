using HelloDotnetTen.ClassLibrary1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// ARCHITECTURAL FIX:
// The consumer no longer knows how to construct Class1 or Class2.
// It simply says "I want to use this library" and passes the configuration root.
builder.Services.AddHelloDotnetLibrary(builder.Configuration);

var app = builder.Build();

// We resolve the Interfaces, not the concrete types
var c1 = app.Services.GetRequiredService<IClass1>();
var c2 = app.Services.GetRequiredService<IClass2>();

Console.WriteLine($"Class1 length: {c1.GetLengthOfInjectedProperty()}");
Console.WriteLine($"Class2 length: {c2.GetLengthOfInjectedProperty()}");
