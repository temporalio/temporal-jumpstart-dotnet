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

public class EndToEndContextPropagationTests : TestBase
{
    private static Identity? _activityIdentity;
    private static IPrincipal? _activityPrincipal;
    private static readonly Dictionary<string, Identity> _capturedActivityIdentities = new();
    private static readonly Dictionary<string, IPrincipal> _capturedPrincipals = new();
    
    public EndToEndContextPropagationTests(ITestOutputHelper output) : base(output)
    {
        _activityIdentity = null;
        _activityPrincipal = null;
        _capturedActivityIdentities.Clear();
        _capturedPrincipals.Clear();
    }

    [Activity]
    public static Task ProcessEntity(string entityId)
    {
        _activityIdentity = IdentityContext.User.Value;
        _activityPrincipal = ApplicationContext.CurrentPrincipal.Value;
        return Task.CompletedTask;
    }

    [Activity]
    public static Task ProcessForUser(string userId, string workflowId)
    {
        var identity = IdentityContext.User.Value;
        var principal = ApplicationContext.CurrentPrincipal.Value;
        
        if (identity != null)
        {
            lock (_capturedActivityIdentities)
            {
                _capturedActivityIdentities[workflowId] = identity;
            }
        }
        
        if (principal != null)
        {
            lock (_capturedPrincipals)
            {
                _capturedPrincipals[workflowId] = principal;
            }
        }
        
        return Task.CompletedTask;
    }

    [Fact]
    public async Task EndToEnd_Should_PropagateIdentityFromServiceToWorkerThroughEntireFlow()
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
        
        // Simulate a service call with a specific user identity
        var serviceIdentity = new Identity 
        { 
            ClientId = "web-app-client", 
            UserId = "john.doe@company.com" 
        };
        
        // Captured values to verify propagation
        Identity? workflowIdentity = null;
        
        var workerOptions = new TemporalWorkerOptions("test")
            .AddWorkflow<EndToEndWorkflow>()
            .AddActivity(ProcessEntity);
        
        using var worker = new TemporalWorker(env.Client, workerOptions);
        
        await worker.ExecuteAsync(async () =>
        {
            // Step 1: Service call initiates workflow with user identity
            IdentityContext.User.Value = serviceIdentity;
            
            var result = await env.Client.ExecuteWorkflowAsync(
                (EndToEndWorkflow wf) => wf.ExecuteAsync("entity-123"),
                new WorkflowOptions(
                    id: $"entity-processing-{Guid.NewGuid()}",
                    taskQueue: "test"));
            
            workflowIdentity = result;
        });
        
        // Verify identity propagation at each stage
        
        // 1. Workflow should have received the original service identity
        Assert.NotNull(workflowIdentity);
        Assert.Equal(serviceIdentity.ClientId, workflowIdentity.ClientId);
        Assert.Equal(serviceIdentity.UserId, workflowIdentity.UserId);
        
        // 2. Activity should have received the same identity
        Assert.NotNull(_activityIdentity);
        Assert.Equal(serviceIdentity.ClientId, _activityIdentity.ClientId);
        Assert.Equal(serviceIdentity.UserId, _activityIdentity.UserId);
        
        // 3. ApplicationContextInterceptor should have created a principal from the identity
        Assert.NotNull(_activityPrincipal);
        Assert.NotNull(_activityPrincipal.Identity);
        Assert.Equal("john.doe@company.com : web-app-client", _activityPrincipal.Identity.Name);
        Assert.True(_activityPrincipal.IsInRole("admin"));
        Assert.True(_activityPrincipal.IsInRole("user"));
    }

    [Fact]
    public async Task EndToEnd_Should_HandleMultipleConcurrentWorkflowsWithDifferentIdentities()
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
        
        var user1Identity = new Identity { ClientId = "client1", UserId = "user1@company.com" };
        var user2Identity = new Identity { ClientId = "client2", UserId = "user2@company.com" };
        
        var capturedWorkflowIdentities = new Dictionary<string, Identity>();
        
        var workerOptions = new TemporalWorkerOptions("test")
            .AddWorkflow<UserSpecificWorkflow>()
            .AddActivity(ProcessForUser);
        
        using var worker = new TemporalWorker(env.Client, workerOptions);
        
        await worker.ExecuteAsync(async () =>
        {
            var tasks = new List<Task>();
            
            // Start workflow for user1
            IdentityContext.User.Value = user1Identity;
            var workflow1Id = $"user1-{Guid.NewGuid()}";
            tasks.Add(env.Client.ExecuteWorkflowAsync(
                (UserSpecificWorkflow wf) => wf.ExecuteAsync("user1", workflow1Id),
                new WorkflowOptions(id: workflow1Id, taskQueue: "test")));
            
            // Start workflow for user2
            IdentityContext.User.Value = user2Identity;
            var workflow2Id = $"user2-{Guid.NewGuid()}";
            tasks.Add(env.Client.ExecuteWorkflowAsync(
                (UserSpecificWorkflow wf) => wf.ExecuteAsync("user2", workflow2Id),
                new WorkflowOptions(id: workflow2Id, taskQueue: "test")));
            
            await Task.WhenAll(tasks);
            
            // Get the captured workflow identities  
            foreach (var task in tasks.Cast<Task<string>>())
            {
                var result = task.Result;
                var parts = result.Split(':');
                if (parts.Length >= 2)
                {
                    var workflowId = parts[1].Trim();
                    var userId = parts[0].Replace("Processed for ", "").Trim();
                    
                    if (userId == "user1")
                    {
                        capturedWorkflowIdentities[workflowId] = user1Identity;
                    }
                    else if (userId == "user2")
                    {
                        capturedWorkflowIdentities[workflowId] = user2Identity;
                    }
                }
            }
        });
        
        // Verify each workflow maintained its own identity context
        Assert.Equal(2, _capturedActivityIdentities.Count);
        Assert.Equal(2, _capturedPrincipals.Count);
        
        // Verify contexts are correct for each user
        Assert.Contains(_capturedActivityIdentities.Values, id => id.ClientId == "client1" && id.UserId == "user1@company.com");
        Assert.Contains(_capturedActivityIdentities.Values, id => id.ClientId == "client2" && id.UserId == "user2@company.com");
        
        Assert.Contains(_capturedPrincipals.Values, p => p.Identity.Name == "user1@company.com : client1");
        Assert.Contains(_capturedPrincipals.Values, p => p.Identity.Name == "user2@company.com : client2");
    }

    [Workflow]
    public class EndToEndWorkflow
    {
        [WorkflowRun]
        public async Task<Identity?> ExecuteAsync(string entityId)
        {
            var workflowIdentity = IdentityContext.User.Value;
            
            // Execute activity
            await Workflow.ExecuteActivityAsync(
                () => ProcessEntity(entityId),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(30) });
            
            return workflowIdentity;
        }
    }

    [Workflow]
    public class UserSpecificWorkflow
    {
        [WorkflowRun]
        public async Task<string> ExecuteAsync(string userId, string workflowId)
        {
            await Workflow.ExecuteActivityAsync(
                () => ProcessForUser(userId, workflowId),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(10) });
            
            return $"Processed for {userId}: {workflowId}";
        }
    }
}