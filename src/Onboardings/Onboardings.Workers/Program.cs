using System.Diagnostics;
using Onboardings.Domain.Clients.Crm;
using Onboardings.Domain.Clients.Email;
using Onboardings.Domain.Clients.Temporal;
using Onboardings.Domain.Workflows;
using Onboardings.Domain.Workflows.OnboardEntity;
using Onboardings.Domain.Workflows.OnboardEntity.Activities;
using Temporalio.Extensions.Hosting;
using Temporalio.Workflows;

namespace Onboardings.Workers
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await RunIoCApp(args);
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
            builder.Services.AddSingleton<IEmailClient, InMemoryEmailClient>();

            // configure our Worker
            builder.Services.AddHostedTemporalWorker(temporalConfig.Worker.TaskQueue)
                .ConfigureOptions(o => { o.ConfigureService(temporalConfig); })
                .AddScopedActivities<RegistrationActivities>()
                .AddScopedActivities<NotificationActivities>()
                .AddWorkflow<OnboardEntity>()
                .AddWorkflow<Ping>();

            var host = builder.Build();
            var lf = host.Services.GetService<ILoggerFactory>();
            Debug.Assert(lf != null);
            var logger = lf.CreateLogger<Program>();
            logger.LogInformation(@"Starting Temporal Worker connecting to " +
                                  $"Namespace '{temporalConfig.Connection.Namespace}@{temporalConfig.Connection.Target}'" +
                                  $" subscribed to TaskQueue: '{temporalConfig.Worker.TaskQueue}'");
            await host.RunAsync();
        }
    }
}