using Temporal.Curriculum.Workflows.Domain.Orchestrations;
using Temporal.Curriculum.Workflows.Messages.Orchestrations;
using Temporalio.Client;
using Temporalio.Exceptions;
using Temporalio.Worker;

namespace Temporal.Curriculum.Workflows.Tests.Orchestrations;
using Xunit;
using Xunit.Abstractions;
using Temporalio.Testing;
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

    public StartOnboardingTests(ITestOutputHelper output) : base(output)
    {
    }
}