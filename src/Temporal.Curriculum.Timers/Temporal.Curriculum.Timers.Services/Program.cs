using System.Diagnostics;
using Temporal.Curriculum.Timers.Domain.Clients.Crm;
using Temporal.Curriculum.Timers.Domain.Clients.Email;
using Temporal.Curriculum.Timers.Domain.Clients.Temporal;
using Temporal.Curriculum.Timers.Domain.Integrations;
using Temporal.Curriculum.Timers.Domain.Orchestrations;
using Temporalio.Extensions.Hosting;

using IntegrationsHandlers = Temporal.Curriculum.Timers.Domain.Integrations.Handlers;
using NotificationHandlers = Temporal.Curriculum.Timers.Domain.Notifications.Handlers;
namespace Temporal.Curriculum.Timers.Services
{
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
            builder.Services.AddSingleton<IEmailClient, InMemoryEmailClient>();

            // configure our Worker
            builder.Services.AddHostedTemporalWorker(temporalConfig.Worker.TaskQueue)
                .ConfigureOptions(o => { o.ConfigureService(temporalConfig); })
                .AddScopedActivities<IntegrationsHandlers>()
                .AddScopedActivities<NotificationHandlers>()
                .AddWorkflow<OnboardEntity>();

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