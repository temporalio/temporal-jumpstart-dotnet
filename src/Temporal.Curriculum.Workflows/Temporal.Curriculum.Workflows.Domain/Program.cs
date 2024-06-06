using Temporal.Curriculum.Workflows.Domain;
using Temporal.Curriculum.Workflows.Domain.Clients;

// var builder = Host.CreateApplicationBuilder(args);
// builder.UseStartup
// builder.Services.AddHostedService<Worker>();
//
// var host = builder.Build();
// host.Run();

public class Program
{

    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.AddConsole();
        builder.Services.AddOptions<TemporalConfig>().BindConfiguration("Temporal");
        builder.Services.AddSingleton<ITemporalClientFactory, TemporalClientFactory>();
        builder.Services.AddHostedService<Worker>();
        var host = builder.Build();
        // run the web app
        host.Run();
    }
}