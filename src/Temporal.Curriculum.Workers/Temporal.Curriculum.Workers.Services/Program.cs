using System.Diagnostics;
using Temporal.Curriculum.Workers.Domain.Clients.Crm;
using Temporal.Curriculum.Workers.Domain.Clients.Temporal;
using Temporal.Curriculum.Workers.Domain.Integrations;
using Temporal.Curriculum.Workers.Domain.Orchestrations;
using Temporalio.Extensions.Hosting;

namespace Temporal.Curriculum.Workers.Services;

public static class ProgramExtensions
{
    // public static IServiceCollection AddTemporal(this IServiceCollection services)
    // {
    //     // services.AddHostedTemporalWorker()
    //     // services.AddOptions<TemporalConfig>().BindConfiguration("Temporal");
    //     // services.ConfigureOptions<>()
    //     // services.BuildServiceProvider(new ServiceProviderOptions()
    //     // {
    //     //     
    //     // })
    //     // var opts = services.GetRequiredService<IOptions<TemporalConfig>>();
    // }
    // public static ITemporalWorkerServiceOptionsBuilder AddHostedTemporalWorkerFromConfig(
    //     this IServiceCollection services) =>
    //     services.AddHostedTemporalWorker()
    //     services.AddHostedTemporalWorker(taskQueue, buildId).ConfigureOptions(options =>
    //         options.ClientOptions = new(clientTargetHost) { Namespace = clientNamespace });
}

public class Program
{
    public static async Task Main(string[] args)
    {
        await RunIoCApp(args);
        // RunApp(args);
    }

    private static async Task RunIoCApp(string[] args)
    {
        const string temporalConfigSection = "Temporal";
        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration.Sources.Clear();
        builder.Logging.AddConsole();
        // DOTNET_ENVIRONMENT variable isnt working with appsettings files so we are being explicit here
        builder.Configuration
            .AddJsonFile(Path.GetFullPath($"../../../config/appsettings.{builder.Environment.EnvironmentName}.json"));
        var temporalConfig = builder.Configuration.GetRequiredSection(temporalConfigSection).Get<TemporalConfig>();
        Debug.Assert(temporalConfig != null, nameof(temporalConfig) + " != null");
        builder.Configuration.AddEnvironmentVariables();

        // expose OptionsPattern in services
        builder.Services.AddOptions<TemporalConfig>().BindConfiguration(temporalConfigSection);
        builder.Services.AddSingleton<ICrmClient, InMemoryCrmClient>();

        // configure our Worker
        builder.Services.AddHostedTemporalWorker(temporalConfig.Worker.TaskQueue)
            .ConfigureOptions(o => { o.ConfigureService(temporalConfig); })
            .AddScopedActivities<Handlers>()
            .AddWorkflow<OnboardEntity>();

        var host = builder.Build();
        var lf = host.Services.GetService<ILoggerFactory>();
        Debug.Assert(lf != null);
        var logger = lf.CreateLogger<Program>();
        logger.LogInformation("Starting Temporal Worker connecting to " +
                              $"Namespace '{temporalConfig.Connection.Namespace}@{temporalConfig.Connection.Target}'" +
                              $" subscribed to TaskQueue: '{temporalConfig.Worker.TaskQueue}'");
        await host.RunAsync();
    }
}