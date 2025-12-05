using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var env = builder.Environment;

Console.WriteLine(env.EnvironmentName);

if (env.IsDevelopment())
{
    Console.WriteLine("Development environment");
}
else
{
    Console.WriteLine("Production environment");
}
