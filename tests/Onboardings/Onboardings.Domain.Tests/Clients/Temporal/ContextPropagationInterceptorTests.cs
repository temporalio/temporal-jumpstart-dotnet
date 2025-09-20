using Onboardings.Domain.Clients.Temporal;
using Onboardings.Domain.Workflows;
using Temporalio.Activities;
using Temporalio.Client;
using Temporalio.Converters;
using Temporalio.Testing;
using Temporalio.Worker;
using Temporalio.Workflows;
using Xunit.Abstractions;

namespace Onboardings.Domain.Tests.Clients.Temporal;

public class ContextPropagationInterceptorTests : TestBase
{
    private static Identity? _capturedActivityIdentity;
    private static readonly List<Identity?> _capturedIdentities = new();
    
    public ContextPropagationInterceptorTests(ITestOutputHelper output) : base(output)
    {
        _capturedActivityIdentity = null;
        _capturedIdentities.Clear();
    }

    [Activity]
    public static Task CaptureIdentity()
    {
        _capturedActivityIdentity = IdentityContext.User.Value;
        return Task.CompletedTask;
    }

    [Activity]
    public static async Task CaptureIdentityWithDelay(string activityId)
    {
        await Task.Delay(100);
        lock (_capturedIdentities)
        {
            _capturedIdentities.Add(IdentityContext.User.Value);
        }
    }

    [Fact]
    public async Task ContextPropagationInterceptor_Should_PropagateIdentityFromClientToWorkflow()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync(
            new WorkflowEnvironmentStartTimeSkippingOptions
            {
                Interceptors = [
                    new ContextPropagationInterceptor<Identity>(
                        IdentityContext.User, DataConverter.Default.PayloadConverter)],
            }
        );
        
        // Set up identity in the client context
        var expectedIdentity = new Identity { ClientId = "test-client", UserId = "test-user" };
        IdentityContext.User.Value = expectedIdentity;
        
        Identity? capturedIdentity = null;
        
        var workerOptions = new TemporalWorkerOptions("test")
            .AddWorkflow<IdentityWorkflow>();
        
        using var worker = new TemporalWorker(env.Client, workerOptions);
        
        await worker.ExecuteAsync(async () =>
        {
            var result = await env.Client.ExecuteWorkflowAsync(
                (IdentityWorkflow wf) => wf.ExecuteAsync(),
                new WorkflowOptions(id: Guid.NewGuid().ToString(), taskQueue: "test"));
            
            capturedIdentity = result;
        });
        
        Assert.NotNull(capturedIdentity);
        Assert.Equal(expectedIdentity.ClientId, capturedIdentity.ClientId);
        Assert.Equal(expectedIdentity.UserId, capturedIdentity.UserId);
    }

    [Fact]
    public async Task ContextPropagationInterceptor_Should_PropagateIdentityFromWorkflowToActivity()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync(
            new WorkflowEnvironmentStartTimeSkippingOptions
            {
                Interceptors = [
                    new ContextPropagationInterceptor<Identity>(
                        IdentityContext.User, DataConverter.Default.PayloadConverter)],
            }
        );
        
        var expectedIdentity = new Identity { ClientId = "workflow-client", UserId = "workflow-user" };
        IdentityContext.User.Value = expectedIdentity;
        
        var workerOptions = new TemporalWorkerOptions("test")
            .AddWorkflow<ActivityTestWorkflow>()
            .AddActivity(CaptureIdentity);
        
        using var worker = new TemporalWorker(env.Client, workerOptions);
        
        await worker.ExecuteAsync(async () =>
        {
            await env.Client.ExecuteWorkflowAsync(
                (ActivityTestWorkflow wf) => wf.ExecuteAsync(),
                new WorkflowOptions(id: Guid.NewGuid().ToString(), taskQueue: "test"));
        });
        
        Assert.NotNull(_capturedActivityIdentity);
        Assert.Equal(expectedIdentity.ClientId, _capturedActivityIdentity.ClientId);
        Assert.Equal(expectedIdentity.UserId, _capturedActivityIdentity.UserId);
    }

    [Fact]
    public async Task ContextPropagationInterceptor_Should_HandleNullIdentityGracefully()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync(
            new WorkflowEnvironmentStartTimeSkippingOptions
            {
                Interceptors = [
                    new ContextPropagationInterceptor<Identity>(
                        IdentityContext.User, DataConverter.Default.PayloadConverter)],
            }
        );
        
        // Set null identity
        IdentityContext.User.Value = null;
        
        Identity? capturedIdentity = null;
        
        var workerOptions = new TemporalWorkerOptions("test")
            .AddWorkflow<IdentityWorkflow>();
        
        using var worker = new TemporalWorker(env.Client, workerOptions);
        
        await worker.ExecuteAsync(async () =>
        {
            var result = await env.Client.ExecuteWorkflowAsync(
                (IdentityWorkflow wf) => wf.ExecuteAsync(),
                new WorkflowOptions(id: Guid.NewGuid().ToString(), taskQueue: "test"));
            
            capturedIdentity = result;
        });
        
        Assert.Null(capturedIdentity);
    }

    [Fact]
    public async Task ContextPropagationInterceptor_Should_IsolateAsyncLocalBetweenConcurrentActivities()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync(
            new WorkflowEnvironmentStartTimeSkippingOptions
            {
                Interceptors = [
                    new ContextPropagationInterceptor<Identity>(
                        IdentityContext.User, DataConverter.Default.PayloadConverter)],
            }
        );
        
        var identity1 = new Identity { ClientId = "client1", UserId = "user1" };
        
        var workerOptions = new TemporalWorkerOptions("test")
            .AddWorkflow<ConcurrentActivitiesWorkflow>()
            .AddActivity(CaptureIdentityWithDelay);
        
        using var worker = new TemporalWorker(env.Client, workerOptions);
        
        // Test with first identity
        IdentityContext.User.Value = identity1;
        
        await worker.ExecuteAsync(async () =>
        {
            await env.Client.ExecuteWorkflowAsync(
                (ConcurrentActivitiesWorkflow wf) => wf.ExecuteAsync(),
                new WorkflowOptions(id: Guid.NewGuid().ToString(), taskQueue: "test"));
        });
        
        // Both activities should have captured the same identity that was set in the workflow context
        Assert.Equal(2, _capturedIdentities.Count);
        Assert.All(_capturedIdentities, identity =>
        {
            Assert.NotNull(identity);
            Assert.Equal(identity1.ClientId, identity.ClientId);
            Assert.Equal(identity1.UserId, identity.UserId);
        });
    }

    [Workflow]
    public class IdentityWorkflow
    {
        [WorkflowRun]
        public Task<Identity?> ExecuteAsync()
        {
            // Capture the identity within the workflow context
            return Task.FromResult(IdentityContext.User.Value);
        }
    }

    [Workflow]
    public class ActivityTestWorkflow
    {
        [WorkflowRun]
        public async Task ExecuteAsync()
        {
            await Workflow.ExecuteActivityAsync(
                () => CaptureIdentity(),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(10) });
        }
    }

    [Workflow]
    public class ConcurrentActivitiesWorkflow
    {
        [WorkflowRun]
        public async Task ExecuteAsync()
        {
            var task1 = Workflow.ExecuteActivityAsync(
                () => CaptureIdentityWithDelay("activity1"),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(10) });
            
            var task2 = Workflow.ExecuteActivityAsync(
                () => CaptureIdentityWithDelay("activity2"),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(10) });
            
            await Task.WhenAll(task1, task2);
        }
    }
}