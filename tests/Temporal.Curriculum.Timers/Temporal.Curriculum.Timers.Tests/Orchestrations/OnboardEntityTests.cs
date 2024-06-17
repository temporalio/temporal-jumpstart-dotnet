using Temporal.Curriculum.Timers.Domain.Orchestrations;
using Temporal.Curriculum.Timers.Messages.Orchestrations;
using Temporalio.Client;
using Temporalio.Exceptions;
using Temporalio.Testing;
using Temporalio.Worker;
using Xunit.Abstractions;

namespace Temporal.Curriculum.Timers.Tests.Orchestrations;

public class OnboardEntityTests : TestBase
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

    public OnboardEntityTests(ITestOutputHelper output) : base(output)
    {
    }
}