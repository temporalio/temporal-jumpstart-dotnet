using Temporal.Curriculum.Workflows.Domain.Clients;
using Temporal.Curriculum.Workflows.Domain.Orchestrations;
using Temporalio.Worker;

namespace Temporal.Curriculum.Workflows.Domain;

public class Worker : IHostedService
{
    private readonly ILogger<Worker> _logger;
    private readonly ITemporalClientFactory _temporalClientFactory;
    public Worker(ILogger<Worker> logger, ITemporalClientFactory temporalClientFactory)
    {
        _logger = logger;
        _temporalClientFactory = temporalClientFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Microsoft.Extensions.Logging.ILogger logger = null;
        // Cancellation token cancelled on ctrl+c
        using var tokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            tokenSource.Cancel();
            eventArgs.Cancel = true;
        };

        // Create an activity instance with some state
        // var activities = new MyActivities();
        var client = await _temporalClientFactory.CreateClientAsync();
        // Run worker until cancelled
        Console.WriteLine("Running worker");
        using var worker = new TemporalWorker(
            client,
            new TemporalWorkerOptions(taskQueue: "onboardings").
                // AddActivity(activities.SelectFromDatabaseAsync).
                // AddActivity(MyActivities.DoStaticThing).
                AddWorkflow<OnboardEntity>());
        try
        {
            await worker.ExecuteAsync(tokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Worker cancelled");
        }    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}