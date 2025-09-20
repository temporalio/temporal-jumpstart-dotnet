using System.Security.Principal;
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

public class ApplicationContextInterceptorTests : TestBase
{
    private static IPrincipal? _capturedPrincipal;
    private static readonly List<IPrincipal?> _capturedPrincipals = new();
    
    public ApplicationContextInterceptorTests(ITestOutputHelper output) : base(output)
    {
        _capturedPrincipal = null;
        _capturedPrincipals.Clear();
    }

    [Activity]
    public static Task CaptureApplicationContext()
    {
        _capturedPrincipal = ApplicationContext.CurrentPrincipal.Value;
        return Task.CompletedTask;
    }

    [Activity]  
    public static async Task CaptureApplicationContextWithDelay(string activityId)
    {
        await Task.Delay(50);
        lock (_capturedPrincipals)
        {
            _capturedPrincipals.Add(ApplicationContext.CurrentPrincipal.Value);
        }
    }

    [Fact]
    public async Task ApplicationContextInterceptor_Should_SetPrincipalInActivityFromIdentityContext()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync(
            new WorkflowEnvironmentStartTimeSkippingOptions
            {
                Interceptors = [
                    new ContextPropagationInterceptor<Identity>(
                        IdentityContext.User, DataConverter.Default.PayloadConverter),
                    new ApplicationContextInterceptor()],
            }
        );
        
        var expectedIdentity = new Identity { ClientId = "test-client", UserId = "test-user" };
        IdentityContext.User.Value = expectedIdentity;
        
        var workerOptions = new TemporalWorkerOptions("test")
            .AddWorkflow<TestWorkflow>()
            .AddActivity(CaptureApplicationContext);

        
        using var worker = new TemporalWorker(env.Client, workerOptions);
        
        await worker.ExecuteAsync(async () =>
        {
            await env.Client.ExecuteWorkflowAsync(
                (TestWorkflow wf) => wf.ExecuteAsync(),
                new WorkflowOptions(id: Guid.NewGuid().ToString(), taskQueue: "test"));
        });
        
        Assert.NotNull(_capturedPrincipal);
        Assert.NotNull(_capturedPrincipal.Identity);
        Assert.Equal("test-user : test-client", _capturedPrincipal.Identity.Name);
        Assert.True(_capturedPrincipal.IsInRole("admin"));
        Assert.True(_capturedPrincipal.IsInRole("user"));
    }

    [Fact]
    public async Task ApplicationContextInterceptor_Should_HandleNullIdentityGracefully()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync(
            new WorkflowEnvironmentStartTimeSkippingOptions
            {
                Interceptors = [
                    new ContextPropagationInterceptor<Identity>(
                        IdentityContext.User, DataConverter.Default.PayloadConverter),
                    new ApplicationContextInterceptor()],
            }
        );
        
        // Set null identity
        IdentityContext.User.Value = null;
        
        var workerOptions = new TemporalWorkerOptions("test")
            .AddWorkflow<TestWorkflow>()
            .AddActivity(CaptureApplicationContext);
        
        using var worker = new TemporalWorker(env.Client, workerOptions);
        
        await worker.ExecuteAsync(async () =>
        {
            await env.Client.ExecuteWorkflowAsync(
                (TestWorkflow wf) => wf.ExecuteAsync(),
                new WorkflowOptions(id: Guid.NewGuid().ToString(), taskQueue: "test"));
        });
        
        Assert.NotNull(_capturedPrincipal);
        Assert.NotNull(_capturedPrincipal.Identity);
        // When identity is null, the ApplicationContextInterceptor creates " : " as the name
        Assert.Equal(" : ", _capturedPrincipal.Identity.Name);
    }

    [Fact]
    public async Task ApplicationContextInterceptor_Should_IsolateAsyncLocalBetweenActivities()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync(
            new WorkflowEnvironmentStartTimeSkippingOptions
            {
                Interceptors = [
                    new ContextPropagationInterceptor<Identity>(
                        IdentityContext.User, DataConverter.Default.PayloadConverter),
                    new ApplicationContextInterceptor()],
            }
        );
        
        var testIdentity = new Identity { ClientId = "shared-client", UserId = "shared-user" };
        IdentityContext.User.Value = testIdentity;
        
        var workerOptions = new TemporalWorkerOptions("test")
            .AddWorkflow<ConcurrentWorkflow>()
            .AddActivity(CaptureApplicationContextWithDelay);
        
        using var worker = new TemporalWorker(env.Client, workerOptions);
        
        await worker.ExecuteAsync(async () =>
        {
            await env.Client.ExecuteWorkflowAsync(
                (ConcurrentWorkflow wf) => wf.ExecuteAsync(),
                new WorkflowOptions(id: Guid.NewGuid().ToString(), taskQueue: "test"));
        });
        
        Assert.Equal(2, _capturedPrincipals.Count);
        Assert.All(_capturedPrincipals, principal =>
        {
            Assert.NotNull(principal);
            Assert.NotNull(principal.Identity);
            Assert.Equal("shared-user : shared-client", principal.Identity.Name);
            Assert.True(principal.IsInRole("admin"));
            Assert.True(principal.IsInRole("user"));
        });
    }

    [Workflow]
    public class TestWorkflow
    {
        [WorkflowRun]
        public async Task ExecuteAsync()
        {
            await Workflow.ExecuteActivityAsync(
                () => CaptureApplicationContext(),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(10) });
        }
    }

    [Workflow]
    public class ConcurrentWorkflow
    {
        [WorkflowRun]
        public async Task ExecuteAsync()
        {
            var task1 = Workflow.ExecuteActivityAsync(
                () => CaptureApplicationContextWithDelay("activity1"),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(10) });
            
            var task2 = Workflow.ExecuteActivityAsync(
                () => CaptureApplicationContextWithDelay("activity2"),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(10) });
            
            await Task.WhenAll(task1, task2);
        }
    }
}