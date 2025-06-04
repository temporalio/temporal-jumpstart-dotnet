namespace Onboardings.Api;

public abstract class Program
{
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        // run the web app
        host.Run();
    }


    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
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
}