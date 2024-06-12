using Temporal.Curriculum.Activities.Domain.Clients;
using Temporal.Curriculum.Activities.Domain.Clients.Crm;
using Temporal.Curriculum.Activities.Domain.Clients.Temporal;
using Temporal.Curriculum.Activities.Domain.Integrations;
using Temporal.Curriculum.Activities.Domain.Orchestrations;
using Temporalio.Worker;

namespace Temporal.Curriculum.Activities.Domain;

/*
 * This simple Temporal Worker registers a lone Workflow definition.
 * Workers and their configuration will be discussed later in the Curriculum. 
 */
public class Worker : IHostedService
{
    private readonly ILogger<Worker> _logger;
    private readonly ITemporalClientFactory _temporalClientFactory;
    public Worker(ILogger<Worker> logger, 
        ITemporalClientFactory temporalClientFactory)
    {
        _logger = logger;
        _temporalClientFactory = temporalClientFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var client = await _temporalClientFactory.CreateClientAsync();
        // Run worker until cancelled
        Console.WriteLine("Running worker");
        var crmClient = new InMemoryCrmClient();
        var integrationHandlers = new Handlers(crmClient);
        using var worker = new TemporalWorker(
            client,
            new TemporalWorkerOptions(_temporalClientFactory.GetConfig().Worker.TaskQueue)
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