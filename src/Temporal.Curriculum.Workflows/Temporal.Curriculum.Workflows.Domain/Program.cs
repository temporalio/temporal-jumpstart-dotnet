using Temporal.Curriculum.Workflows.Domain;
using Temporal.Curriculum.Workflows.Domain.Clients;

public class Program
{

    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.AddConsole();
        
        // DOTNET_ENVIRONMENT variable isnt working with appsettings files so we are being explicit here
        builder.Configuration
            .SetBasePath(Path.GetFullPath(@"../Config"))
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json")
            .AddEnvironmentVariables().Build();
        builder.Services.Configure<TemporalConfig>(builder.Configuration.GetSection("Temporal"));
        // builder.Configuration.AddJsonFile()
        builder.Services.AddOptions<TemporalConfig>().BindConfiguration("Temporal");
        builder.Services.AddSingleton<ITemporalClientFactory, TemporalClientFactory>();
        builder.Services.AddHostedService<Worker>();
        var host = builder.Build();
        // run the app
        host.Run();
    }
}