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
    public async Task RunAsync_SimpleRun_GivenValidArgs_Succeeds()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();
        var wid = Guid.NewGuid();
        var args = new OnboardEntityRequest(wid.ToString(), "test");
        using var worker = new TemporalWorker(
            env.Client,
            new TemporalWorkerOptions("test").AddWorkflow<OnboardEntity>());

        await worker.ExecuteAsync(async () =>
        {
            await env.Client.ExecuteWorkflowAsync(
                (OnboardEntity wf) => wf.ExecuteAsync(args),
                new WorkflowOptions(id: args.Id, taskQueue: worker.Options.TaskQueue!));
        });
    }

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
    public async Task RunAsync_RegisterCrmEntity()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();
        var args = new OnboardEntityRequest(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        RegisterCrmEntityRequest entityRegistered = null;
        [Activity]
        void RegisterCrmEntity(RegisterCrmEntityRequest req)
        {
            entityRegistered = req;
            Assert.Equal(args.Id, req.Id);
        }
        using var worker = new TemporalWorker(
            env.Client,
            new TemporalWorkerOptions("test").
                AddWorkflow<OnboardEntity>().
                AddActivity(RegisterCrmEntity));
        
        await worker.ExecuteAsync(async () =>
        {
            await env.Client.ExecuteWorkflowAsync(
                (OnboardEntity wf) => wf.ExecuteAsync(args),
                new WorkflowOptions(id: args.Id, taskQueue: worker.Options.TaskQueue!));

        });
        Assert.NotNull(entityRegistered);
        Assert.Equal(args.Id, entityRegistered.Id);
    }

    public StartOnboardingTests(ITestOutputHelper output) : base(output)
    {
    }
}