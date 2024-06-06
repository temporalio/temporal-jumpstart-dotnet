using Temporal.Curriculum.Workflows.Domain.Orchestrations;
using Temporal.Curriculum.Workflows.Messages.Orchestrations;
using Temporalio.Client;
using Temporalio.Worker;

namespace Temporal.Curriculum.Workflows.Tests.Orchestrations;
using Xunit;
using Xunit.Abstractions;
using Temporalio.Testing;
public class StartOnboardingTests : TestBase
{
    [Fact]
    public async Task RunAsync_SimpleRun_Succeeds()
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
            Assert.Equal(42, 42);
        });
        
        
        // var myActivities = new MyActivities();
        // using var worker = new TemporalWorker(
        //     env.Client,
        //     new TemporalWorkerOptions("my-task-queue").
        //         AddActivity(myActivities.SelectFromDatabaseAsync).
        //         AddActivity(MyActivities.DoStaticThing).
        //         AddWorkflow<MyWorkflow>());
        // await worker.ExecuteAsync(async () =>
        // {
        //     // Just run the workflow and confirm output
        //     var result = await env.Client.ExecuteWorkflowAsync(
        //         (MyWorkflow wf) => wf.RunAsync(),
        //         new(id: $"workflow-{Guid.NewGuid()}", taskQueue: worker.Options.TaskQueue!));
        //     Assert.Equal("some-static-value", result);
        // });
    }

    public StartOnboardingTests(ITestOutputHelper output) : base(output)
    {
    }
}