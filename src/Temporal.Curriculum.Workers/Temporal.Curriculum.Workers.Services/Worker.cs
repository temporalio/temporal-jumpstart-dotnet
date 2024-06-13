using Microsoft.Extensions.Options;
using Temporal.Curriculum.Workers.Domain.Clients.Crm;
using Temporal.Curriculum.Workers.Domain.Clients.Temporal;
using Temporal.Curriculum.Workers.Domain.Integrations;
using Temporal.Curriculum.Workers.Domain.Orchestrations;
using Temporalio.Worker;

namespace Temporal.Curriculum.Workers.Services;

/*
 * This simple Temporal Worker registers a lone Workflow definition.
 * Workers and their configuration will be discussed later in the Curriculum. 
 */
public class Worker : IHostedService
{
    private readonly ILogger<Worker> _logger;
    private readonly ITemporalClientFactory _temporalClientFactory;
    private readonly TemporalConfig _temporalConfig;

    public Worker(ILogger<Worker> logger, 
        IOptions<TemporalConfig> temporalConfig,
        ITemporalClientFactory temporalClientFactory)
    {
        _logger = logger;
        _temporalConfig = temporalConfig.Value;
        _temporalClientFactory = temporalClientFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var client = await _temporalClientFactory.CreateClientAsync();
        // Run worker until cancelled
        Console.WriteLine("Running worker");
        var crmClient = new InMemoryCrmClient();
        var integrationHandlers = new Handlers(crmClient);
        var opts = new TemporalWorkerOptions(_temporalConfig.Worker.TaskQueue)
        {
            // Pollers ask for work if Executors are available
            MaxConcurrentActivityTaskPolls = _temporalConfig.Worker.Capacity.MaxConcurrentActivityTaskPollers,
            MaxConcurrentWorkflowTaskPolls =_temporalConfig.Worker.Capacity.MaxConcurrentWorkflowTaskPollers,
            // Executors do the work the Pollers hand them
            MaxConcurrentWorkflowTasks = _temporalConfig.Worker.Capacity.MaxConcurrentWorkflowTaskExecutors,
            MaxConcurrentActivities = _temporalConfig.Worker.Capacity.MaxConcurrentActivityExecutors,
            MaxConcurrentLocalActivities = _temporalConfig.Worker.Capacity.MaxConcurrentLocalActivityExecutors,
            // Rate Limiting configuration
            MaxActivitiesPerSecond = _temporalConfig.Worker.RateLimits.MaxWorkerActivitiesPerSecond,
            MaxTaskQueueActivitiesPerSecond = _temporalConfig.Worker.RateLimits.MaxTaskQueueActivitiesPerSecond,
        };

        using var worker = new TemporalWorker(
            client,opts
                .AddWorkflow<OnboardEntity>()
                .AddAllActivities(integrationHandlers)
            );
        try
        {
            await worker.ExecuteAsync(cancellationToken);
        }
        catch (OperationCanceledException e)
        {
            Console.WriteLine(e.GetType().FullName);
            Console.WriteLine(e.Message);
        }   
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("StopAsync");
        return Task.CompletedTask;
    }
}