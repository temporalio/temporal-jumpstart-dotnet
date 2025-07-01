using Onboardings.Domain.Commands.V1;
using Onboardings.Domain.Workflows.OnboardEntity;
using Onboardings.Domain.Workflows.V2;
using Temporalio.Activities;
using Temporalio.Client;
using Temporalio.Common;
using Temporalio.Exceptions;
using Temporalio.Testing;
using Temporalio.Worker;
using Xunit.Abstractions;


namespace Onboardings.Domain.Tests.Workflows;

public class OnboardEntityReplayTests(ITestOutputHelper output, ITestOutputHelper testOutputHelper)
    : TestBase(output)
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;


    [Fact]
    public async Task ExecuteAsync_GivenSameType_ShouldReplayOK()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();
        var args = new OnboardEntityRequest
        {
            Id = Guid.NewGuid().ToString(), Value = Guid.NewGuid().ToString(), SkipApproval = true,
        };

        RegisterCrmEntityRequest requested = null;

        var workerOptions = new TemporalWorkerOptions("test");
        // let's run the test with the new execution 
        workerOptions.AddWorkflow<OnboardEntityV1>();

        /* There are a number of ways to mock the activity.
         Here is an inline example
         */
        [Activity]
        Task RegisterCrmEntity(RegisterCrmEntityRequest req)
        {
            requested = req;
            return Task.CompletedTask;
        }

        workerOptions.AddActivity(RegisterCrmEntity);

        /* Or here we can create an activity definition */
        var act = [Activity("RegisterCrmEntity")](RegisterCrmEntityRequest req) =>
        {
            requested = req;
            return Task.CompletedTask;
        };
        // workerOptions.AddActivity(act);

        using var worker = new TemporalWorker(
            env.Client,
            workerOptions
        );

        WorkflowHistory history = null; 
        await worker.ExecuteAsync(async () =>
        {
            await env.Client.ExecuteWorkflowAsync(
                (OnboardEntityV1 wf) => wf.ExecuteAsync(args),
                new WorkflowOptions(id: args.Id, taskQueue: worker.Options.TaskQueue!));
            var handle = env.Client.GetWorkflowHandle(args.Id);
            history = await handle.FetchHistoryAsync();
        });
        // Now we make sure the old executions will still work with new code..but here it is the same code :) 
        var replayer =
            new WorkflowReplayer(new WorkflowReplayerOptions { DebugMode = true, }.AddWorkflow<OnboardEntityV1>());
        Assert.NotNull(history);
        var result = await replayer.ReplayWorkflowAsync(history, false);
        Assert.Null(result.ReplayFailure);
    }
    [Fact]
    public async Task ExecuteAsync_Unpatched_GivenSameType_ShouldFailReplayWithNDE()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();
        var args = new OnboardEntityRequest
        {
            Id = Guid.NewGuid().ToString(), Value = Guid.NewGuid().ToString(), 
            SkipApproval = true,
        };

        RegisterCrmEntityRequest requested = null;

        var workerOptions = new TemporalWorkerOptions("test");
        // let's run the test with the new execution 
        workerOptions.AddWorkflow<OnboardEntityV1>();

        /* There are a number of ways to mock the activity.
         Here is an inline example
         */
        [Activity]
        Task RegisterCrmEntity(RegisterCrmEntityRequest req)
        {
            requested = req;
            return Task.CompletedTask;
        }

        workerOptions.AddActivity(RegisterCrmEntity);

        /* Or here we can create an activity definition */
        var act = [Activity("RegisterCrmEntity")](RegisterCrmEntityRequest req) =>
        {
            requested = req;
            return Task.CompletedTask;
        };
        // workerOptions.AddActivity(act);

        using var worker = new TemporalWorker(
            env.Client,
            workerOptions
        );

        WorkflowHistory history = null; 
        await worker.ExecuteAsync(async () =>
        {
            var handle = await env.Client.StartWorkflowAsync<OnboardEntityV1>(wf => wf.ExecuteAsync(args),
                new WorkflowOptions(args.Id, worker.Options.TaskQueue!));
            await handle.GetResultAsync(followRuns: false);
            
            history = await handle.FetchHistoryAsync();
        });
        // Now we make sure the old executions will still work with new code
        var replayer =
            new WorkflowReplayer(
                new WorkflowReplayerOptions { DebugMode = true, }.AddWorkflow<OnboardEntityUnpatched>());
        Assert.NotNull(history);
        await Assert.ThrowsAsync<WorkflowNondeterminismException>(async () =>
        {
            await replayer.ReplayWorkflowAsync(history);
        });
    }
    [Fact]
    public async Task ExecuteAsync_Patched_GivenSameType_ShouldFailReplayWithNDE()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();
        var args = new OnboardEntityRequest
        {
            Id = Guid.NewGuid().ToString(), Value = Guid.NewGuid().ToString(), 
            SkipApproval = true,
        };

        RegisterCrmEntityRequest requested = null;

        var workerOptions = new TemporalWorkerOptions("test");
        // let's run the test with the new execution 
        workerOptions.AddWorkflow<OnboardEntityV1>();

        /* There are a number of ways to mock the activity.
         Here is an inline example
         */
        [Activity]
        Task RegisterCrmEntity(RegisterCrmEntityRequest req)
        {
            requested = req;
            return Task.CompletedTask;
        }

        workerOptions.AddActivity(RegisterCrmEntity);

        /* Or here we can create an activity definition */
        var act = [Activity("RegisterCrmEntity")](RegisterCrmEntityRequest req) =>
        {
            requested = req;
            return Task.CompletedTask;
        };
        // workerOptions.AddActivity(act);

        using var worker = new TemporalWorker(
            env.Client,
            workerOptions
        );

        WorkflowHistory history = null; 
        await worker.ExecuteAsync(async () =>
        {
            var handle = await env.Client.StartWorkflowAsync<OnboardEntityV1>(wf => wf.ExecuteAsync(args),
                new WorkflowOptions(args.Id, worker.Options.TaskQueue!));
            await handle.GetResultAsync(followRuns: false);
            
            history = await handle.FetchHistoryAsync();
        });
        // Now we make sure the old executions will still work with new code
        var replayer =
            new WorkflowReplayer(
                new WorkflowReplayerOptions { DebugMode = true, }.AddWorkflow<OnboardEntityPatched>());
        Assert.NotNull(history);
        var result = await replayer.ReplayWorkflowAsync(history, false);
        Assert.Null(result.ReplayFailure);
    }
}