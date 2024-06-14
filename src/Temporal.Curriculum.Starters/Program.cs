using Temporal.Curriculum.Starters.Config;
using Temporalio.Client;

namespace Temporal.Curriculum.Starters;

public class Program
{

    public static async  Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        // run the web app
        await host.RunAsync();
    }


    public static IHostBuilder CreateHostBuilder(string[] args) =>
        
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                webBuilder.ConfigureAppConfiguration(c =>
                {
                    c.AddJsonFile(Path.GetFullPath($"../../config/appsettings.{env}.json"));
                    c.AddEnvironmentVariables().Build();
                });
                webBuilder.UseStartup<Startup>();
            });
}