using System.Diagnostics;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Temporal.Curriculum.Workers.Domain.Clients.Crm;
using Temporal.Curriculum.Workers.Domain.Clients.Temporal;
using Temporal.Curriculum.Workers.Domain.Integrations;
using Temporal.Curriculum.Workers.Domain.Orchestrations;
using Temporalio.Client;
using Temporalio.Extensions.Hosting;
using Temporalio.Worker;

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
        builder.Services.AddHostedTemporalWorker( temporalConfig.Worker.TaskQueue, null)
            .ConfigureOptions(o =>
            {
                o.ConfigureService(temporalConfig);
            })
            .AddScopedActivities<Handlers>()
            .AddWorkflow<OnboardEntity>();
        
        var host = builder.Build();
        Console.WriteLine("Starting worker...");
        await host.RunAsync();
        
//      
        // builder.Services.AddOptions<TemporalConfig>().BindConfiguration("Temporal");

        
           // builder.Services.AddSingleton<ICrmClient, InMemoryCrmClient>((ctx, svc) => {});
//         builder.Services.add
//         builder.Services.AddScoped<Handlers>();
//         builder.Services.AddSingleton<object>((ctx) =>
//         {
//             var opts = ctx.GetRequiredService<IOptions<TemporalConfig>>();
//             var cfg = opts.Value;
//             builder.Services.AddHostedTemporalWorker(cfg.Connection.Target).ConfigureOptions(o =>
//             {
//                 o.ClientOptions = new TemporalClientConnectOptions(cfg.Connection.Target)
//             });
//                 
//                 ConfigureOptions(o =>
//             {
//                 o.ClientOptions.Namespace = cfg.Connection.Namespace;
//                 o.ClientOptions.Tls = new TlsOptions()
//                 {
//                     ClientCert = cfg.Connection.Mtls.CertChainFile;
//                 }
//             });
//         })
//         builder.Services.AddHostedTemporalWorker()
//         builder.Services.AddTemporalClient() 
//         hostBuilder.Configuration
//         hostBuilder.ConfigureLogging(ctx => ctx.AddSimpleConsole().SetMinimumLevel(LogLevel.Information))
//             .ConfigureHostConfiguration(b =>
//             {
//                 b.AddJsonFile(
//                         Path.GetFullPath($"../../../config/appsettings.{builder.Environment.EnvironmentName}.json"))
//                     .AddEnvironmentVariables().Build();
//             })
//             .ConfigureServices(ctx =>
//             {
//                 ctx.
//                 ctx.Configure<TemporalConfig>(ctx.Configuration.GetSection("Temporal"));
//                 ctx.AddOptions<TemporalConfig>().BindConfiguration("Temporal");
//             })
//             
//             
//             .ConfigureServices(ctx =>
//             {
//                 c
//             })
//         var builder = Host.CreateApplicationBuilder(args);
// // Add a hosted Temporal worker which returns a builder to add activities and workflows
//         builder.Services.
//             AddTemporalClient(c =>
//             {
//                 cfg = builder.Services.
//                 
//             }).
//             AddHostedTemporalWorker(
//                 "my-temporal-host:7233",
//                 "my-namespace",
//                 "my-task-queue").
//             AddScopedActivities<MyActivityClass>().
//             AddWorkflow<MyWorkflow>();
//
//         var b= hostBuilder.Build();
// // Make sure you use RunAsync and not Run, see https://github.com/temporalio/sdk-dotnet/issues/220
//         await hostBuilder.RunAsync();
    }
    public static void RunApp(string[] args)
    {
        
        // var builder = Host.CreateApplicationBuilder(args);
        // builder.Logging.AddConsole();
        //
        // // DOTNET_ENVIRONMENT variable isnt working with appsettings files so we are being explicit here
        // builder.Configuration
        //     .AddJsonFile(Path.GetFullPath($"../../../config/appsettings.{builder.Environment.EnvironmentName}.json"))
        //     .AddEnvironmentVariables().Build();
        // builder.Services.Configure<TemporalConfig>(builder.Configuration.GetSection("Temporal"));
        // builder.Services.AddOptions<TemporalConfig>().BindConfiguration("Temporal");
        // builder.Services.AddSingleton<ITemporalClientFactory, TemporalClientFactory>((ctx) =>
        // {
        // });
        // builder.Services.AddHostedTemporalWorker("","","","")
        // builder.Services.AddScoped<>()
        // // add our stubbed services
        // builder.Services.AddSingleton<ICrmClient, InMemoryCrmClient>();
        //
        // builder.Services.AddScoped<>()
        //
        // builder.Services.AddHostedService<Worker>();
        // var host = builder.Build();
        // // run the app
        // host.Run();
    }
   

}