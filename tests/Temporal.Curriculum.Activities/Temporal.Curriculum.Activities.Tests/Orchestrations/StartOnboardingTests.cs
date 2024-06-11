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
    public async Task RunAsync_RegisterCrmEntity()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();
        var args = new OnboardEntityRequest(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        RegisterCrmEntityRequest entityRegistered = null;
        
        
        /* couple ways to mock the activity.
         here is an inline example
         */
        [Activity]
        Task RegisterCrmEntity(RegisterCrmEntityRequest req)
        {
            entityRegistered = req;
            return  Task.CompletedTask;
        }

        /* here we can create an activity definition */
        var act = [Activity("RegisterCrmEntity")] (RegisterCrmEntityRequest req) =>
        {
            entityRegistered = req;
            return Task.CompletedTask;
        };

        using var worker = new TemporalWorker(
            env.Client,
            new TemporalWorkerOptions("test").AddWorkflow<OnboardEntity>()
                // .AddActivity(act)
                .AddActivity(RegisterCrmEntity));

        foreach (var def in worker.Options.Activities)
        {
            Console.WriteLine(def.Name);
        }
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