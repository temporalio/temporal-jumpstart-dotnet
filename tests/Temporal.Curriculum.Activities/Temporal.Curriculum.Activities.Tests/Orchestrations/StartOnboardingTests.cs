using Temporal.Curriculum.Activities.Domain.Integrations;
using Temporal.Curriculum.Activities.Domain.Orchestrations;
using Temporal.Curriculum.Activities.Messages.Commands;
using Temporal.Curriculum.Activities.Messages.Orchestrations;
using Temporalio.Activities;
using Temporalio.Client;
using Temporalio.Exceptions;
using Temporalio.Testing;
using Temporalio.Worker;
using Xunit.Abstractions;

namespace Temporal.Curriculum.Activities.Tests.Orchestrations;

public class StartOnboardingTests : TestBase
{
    [Fact]
    public async Task RunAsync_SimpleRun_GivenInvalidArgs_FailsFast()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();
        var wid = Guid.NewGuid();
        var emptyValue = "";
        var args = new OnboardEntityRequest(wid.ToString(), emptyValue);
        using var worker = new TemporalWorker(
            env.Client,
            new TemporalWorkerOptions("test").AddWorkflow<OnboardEntity>());

        await worker.ExecuteAsync(async () =>
        {
            await Assert.ThrowsAsync<WorkflowFailedException>(async () =>
            {
                await env.Client.ExecuteWorkflowAsync(
                    (OnboardEntity wf) => wf.ExecuteAsync(args),
                    new WorkflowOptions(id: args.Id, taskQueue: worker.Options.TaskQueue!));
            });
        });
    }

    [Fact]
    public async Task RunAsync_GivenHealthyService_RegistersCrmEntity()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();
        var args = new OnboardEntityRequest(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        RegisterCrmEntityRequest requested = null;

        var workerOptions = new TemporalWorkerOptions("test");
        workerOptions.AddWorkflow<OnboardEntity>();
        
        /* There are a number of ways to mock the activity.
         Here is an inline example
         */
        [Activity]
        Task RegisterCrmEntity(RegisterCrmEntityRequest req)
        {
            requested = req;
            return  Task.CompletedTask;
        }
        workerOptions.AddActivity(RegisterCrmEntity);

        /* Or here we can create an activity definition */
        var act = [Activity("RegisterCrmEntity")] (RegisterCrmEntityRequest req) =>
        {
            requested = req;
            return Task.CompletedTask;
        };
        // workerOptions.AddActivity(act);

        using var worker = new TemporalWorker(
            env.Client,
            workerOptions
        );
        
        await worker.ExecuteAsync(async () =>
        {
            await env.Client.ExecuteWorkflowAsync(
                (OnboardEntity wf) => wf.ExecuteAsync(args),
                new WorkflowOptions(id: args.Id, taskQueue: worker.Options.TaskQueue!));

        });
        Assert.NotNull(requested);
        Assert.Equal(args.Id, requested.Id);
    }
    [Fact]
    public async Task RunAsync_GivenUnhealthyService_IsNotRetryable_FailsEntireOnboarding()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();
        var args = new OnboardEntityRequest(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        RegisterCrmEntityRequest requested = null;

        var workerOptions = new TemporalWorkerOptions("test");
        workerOptions.LoggerFactory = LoggerFactory;
        workerOptions.AddWorkflow<OnboardEntity>();
        
        [Activity]
        Task RegisterCrmEntity(RegisterCrmEntityRequest req)
        {
            requested = req;
            var inner = new TaskCanceledException("synthetic timeout exception", Task.FromCanceled(new CancellationToken(true)).Exception);
            throw new ApplicationFailureException(
                message: "test failure", 
                // providing this seems to override the bubbling up of the ERR_SERVICE_UNRECOVERABLE ErrorType
                inner: inner,
                errorType:Errors.ERR_SERVICE_UNRECOVERABLE,
                nonRetryable: true);
        }
        workerOptions.AddActivity(RegisterCrmEntity);

        using var worker = new TemporalWorker(
            env.Client,
            workerOptions
        );
        
        await worker.ExecuteAsync(async () =>
        {
            WorkflowFailedException e = await Assert.ThrowsAsync<WorkflowFailedException>(async () =>
            {
                await env.Client.ExecuteWorkflowAsync(
                    (OnboardEntity wf) => wf.ExecuteAsync(args),
                    new WorkflowOptions(id: args.Id, taskQueue: worker.Options.TaskQueue!));
            });
            // To get to the ErrorType that is NonRetryable, you must walk the `InnerException` from the WorkflowFailed.
            // Notice that the caller must have foreknowledge that it was an Activity that raised this ErrorType.
            var actEx = Assert.IsType<ActivityFailureException>(e.InnerException);
            var appEx = Assert.IsType<ApplicationFailureException>(actEx.InnerException);
            Assert.Equal(Errors.ERR_SERVICE_UNRECOVERABLE, appEx.ErrorType);
            
            // To get to the underlying Exception root cause, you must walk the BaseException .
            var baseEx = Assert.IsType<ApplicationFailureException>(e.GetBaseException());
            Assert.Equal(typeof(TaskCanceledException).Name, baseEx.ErrorType);
        });
        Assert.NotNull(requested);
        Assert.Equal(args.Id, requested.Id);
    }

    public StartOnboardingTests(ITestOutputHelper output) : base(output)
    {
    }
}