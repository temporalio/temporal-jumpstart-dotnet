using System.CommandLine;
using Jumpstart.Domain.Onboardings.Workflows.V1;
using Microsoft.Extensions.Configuration;
using Onboardings.Domain.Clients.Temporal;
using Onboardings.Domain.Workflows.OnboardEntity;
using Temporalio.Client;

namespace Onboardings.Scenarios;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Start OnboardEntity workflows with sustained load");

        var environmentOption = new Option<string>(
            name: "--environment",
            description: "Environment to use: 'local' or 'cloud'",
            getDefaultValue: () => "local");
        environmentOption.AddAlias("-e");

        var countOption = new Option<int>(
            name: "--count",
            description: "Number of workflows to start per batch",
            getDefaultValue: () => 1);
        countOption.AddAlias("-c");

        var intervalOption = new Option<int>(
            name: "--interval",
            description: "Interval in seconds between batches (0 = run once)",
            getDefaultValue: () => 0);
        intervalOption.AddAlias("-i");

        var valuePrefixOption = new Option<string>(
            name: "--value-prefix",
            description: "Prefix for the workflow value field",
            getDefaultValue: () => "value");
        valuePrefixOption.AddAlias("-v");

        var approveOption = new Option<string>(
            name: "--approve",
            description: "Approval strategy: 'force' (skip approval), 'approved' (approve after 20s), 'rejected' (reject after 20s), 'random' (randomly approve/reject after 20s)",
            getDefaultValue: () => "force");
        approveOption.AddAlias("-a");

        rootCommand.AddOption(environmentOption);
        rootCommand.AddOption(countOption);
        rootCommand.AddOption(intervalOption);
        rootCommand.AddOption(valuePrefixOption);
        rootCommand.AddOption(approveOption);

        rootCommand.SetHandler(async (environment, count, interval, valuePrefix, approve) =>
        {
            await RunWorkflows(environment, count, interval, valuePrefix, approve);
        }, environmentOption, countOption, intervalOption, valuePrefixOption, approveOption);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task RunWorkflows(string environment, int count, int interval, string valuePrefix, string approve)
    {
        // Validate approval strategy
        var validStrategies = new[] { "force", "approved", "rejected", "random" };
        if (!validStrategies.Contains(approve.ToLowerInvariant()))
        {
            throw new ArgumentException($"Invalid approval strategy: {approve}. Valid options: {string.Join(", ", validStrategies)}");
        }

        if (interval > 0)
        {
            Console.WriteLine($"Starting batches of {count} workflow(s) every {interval} seconds using '{environment}' environment...");
            Console.WriteLine($"Approval strategy: {approve}");
            Console.WriteLine("Press Ctrl+C to stop.");
        }
        else
        {
            Console.WriteLine($"Starting {count} OnboardEntity workflow(s) using '{environment}' environment...");
            Console.WriteLine($"Approval strategy: {approve}");
        }

        // Load configuration
        var configPath = environment.ToLowerInvariant() switch
        {
            "cloud" => Path.GetFullPath($"../../../config/appsettings.Cloud.json"),
            "local" => Path.GetFullPath($"../../../config/appsettings.Local.json"),
            _ => throw new ArgumentException($"Unknown environment: {environment}. Use 'local' or 'cloud'.")
        };
        var config = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false, reloadOnChange: false)
            .Build();

        var temporalConfig = config.GetSection("Temporal").Get<TemporalConfig>();
        if (temporalConfig == null)
        {
            throw new InvalidOperationException($"Failed to load Temporal configuration from {configPath}");
        }

        // Configure and connect to Temporal
        var clientOptions = new TemporalClientConnectOptions()
            .ConfigureClient(temporalConfig);

        Console.WriteLine($"Connecting to Temporal at {temporalConfig.Connection.Target}...");
        var client = await TemporalClient.ConnectAsync(clientOptions);
        Console.WriteLine("Connected successfully!");

        int batchNumber = 0;
        int totalStarted = 0;
        var allSignalTasks = new List<Task>();

        do
        {
            batchNumber++;
            var batchStartTime = DateTime.UtcNow;

            if (interval > 0)
            {
                Console.WriteLine($"\n=== Batch {batchNumber} at {batchStartTime:yyyy-MM-dd HH:mm:ss} UTC ===");
            }

            // Start workflows in current batch
            var workflowHandles = new List<(string workflowId, WorkflowHandle<OnboardEntity> handle)>();
            for (int i = 0; i < count; i++)
            {
                var workflowId = $"onboard-entity-{Guid.NewGuid()}";
                var entityId = $"entity-{totalStarted + i + 1}-{DateTime.UtcNow:yyyyMMddHHmmss}";
                var value = $"{valuePrefix}-{totalStarted + i + 1}";

                var skipApproval = approve.ToLowerInvariant() == "force";

                var request = new OnboardEntityRequest
                {
                    Id = entityId,
                    Value = value,
                    Email = $"test-{totalStarted + i + 1}@example.com",
                    Options = new OnboardEntityExecutionOptions
                    {
                        SkipApproval = skipApproval,
                        ApprovalTimeoutSeconds = 60
                    }
                };

                var handle = await StartWorkflowAsync(client, workflowId, request, totalStarted + i + 1);
                workflowHandles.Add((workflowId, handle));

                // Add a small delay between starts to avoid overwhelming the service
                if (count > 10 && i < count - 1)
                {
                    await Task.Delay(50);
                }
            }

            totalStarted += count;

            // Handle approval signals if not using "force" strategy
            if (approve.ToLowerInvariant() != "force")
            {
                var signalTask = Task.Run(async () => await SendApprovalSignals(workflowHandles, approve));
                allSignalTasks.Add(signalTask);
            }

            if (interval > 0)
            {
                Console.WriteLine($"Batch {batchNumber} complete. Total started: {totalStarted}");
                Console.WriteLine($"Waiting {interval} seconds until next batch...");
                await Task.Delay(TimeSpan.FromSeconds(interval));
            }

        } while (interval > 0);

        // Wait for all signal tasks to complete before exiting
        if (allSignalTasks.Count > 0)
        {
            Console.WriteLine($"\nWaiting for all approval signals to be sent...");
            await Task.WhenAll(allSignalTasks);
        }

        Console.WriteLine($"\nSuccessfully started {totalStarted} workflow(s)!");
        Console.WriteLine($"Namespace: {temporalConfig.Connection.Namespace}");
        Console.WriteLine($"Task Queue: {temporalConfig.Worker.TaskQueue}");
    }

    static async Task<WorkflowHandle<OnboardEntity>> StartWorkflowAsync(ITemporalClient client, string workflowId, OnboardEntityRequest request, int index)
    {
        try
        {
            var handle = await client.StartWorkflowAsync(
                (OnboardEntity wf) => wf.ExecuteAsync(request),
                new WorkflowOptions
                {
                    Id = workflowId,
                    TaskQueue = "onboardings",
                });

            Console.WriteLine($"[{index}] Started workflow: {workflowId} (Entity: {request.Id})");
            return handle;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{index}] Failed to start workflow {workflowId}: {ex.Message}");
            throw;
        }
    }

    static async Task SendApprovalSignals(List<(string workflowId, WorkflowHandle<OnboardEntity> handle)> workflowHandles, string strategy)
    {
        // Wait 20 seconds before sending signals
        await Task.Delay(TimeSpan.FromSeconds(20));

        var random = new Random();
        var comments = new[]
        {
            "Approved by automated test",
            "Looks good to me",
            "LGTM - approved",
            "Rejected by automated test",
            "Does not meet requirements",
            "Please revise and resubmit"
        };

        foreach (var (workflowId, handle) in workflowHandles)
        {
            try
            {
                bool shouldApprove = strategy.ToLowerInvariant() switch
                {
                    "approved" => true,
                    "rejected" => false,
                    "random" => random.Next(2) == 0,
                    _ => true
                };

                if (shouldApprove)
                {
                    var approveRequest = new ApproveEntityRequest
                    {
                        Comment = comments[random.Next(0, 3)]
                    };
                    await handle.SignalAsync(wf => wf.ApproveAsync(approveRequest));
                    Console.WriteLine($"[Signal] Approved workflow: {workflowId}");
                }
                else
                {
                    var rejectRequest = new RejectEntityRequest
                    {
                        Comment = comments[random.Next(3, 6)]
                    };
                    await handle.SignalAsync(wf => wf.RejectAsync(rejectRequest));
                    Console.WriteLine($"[Signal] Rejected workflow: {workflowId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Signal] Failed to send signal to {workflowId}: {ex.Message}");
            }
        }
    }
}