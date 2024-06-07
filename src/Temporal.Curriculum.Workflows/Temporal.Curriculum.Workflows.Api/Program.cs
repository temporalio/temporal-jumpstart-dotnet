namespace Temporal.Curriculum.Workflows.Api;

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
                webBuilder.ConfigureAppConfiguration(c =>
                {
                    // use Config for this Curriculum section
                    c.SetBasePath(System.IO.Path.GetFullPath(@"../Config"));
                    c.AddEnvironmentVariables().Build();
                });
                webBuilder.UseStartup<Startup>();
            });
}