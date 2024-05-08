using Temporalio.Client;

namespace Temporal.Curriculum.Starters;

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
                webBuilder.UseStartup<Startup>();
            });
}