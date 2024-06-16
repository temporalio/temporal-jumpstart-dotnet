namespace Temporal.Curriculum.Workers.Api;

public class Program
{

    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        // run the web app
        host.Run();
    }

        
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                webBuilder.ConfigureAppConfiguration(c =>
                {
                    c.AddJsonFile(Path.GetFullPath($"../../../config/appsettings.{env}.json"));
                    c.AddEnvironmentVariables().Build();
                });
                webBuilder.UseStartup<Startup>();
            });
}