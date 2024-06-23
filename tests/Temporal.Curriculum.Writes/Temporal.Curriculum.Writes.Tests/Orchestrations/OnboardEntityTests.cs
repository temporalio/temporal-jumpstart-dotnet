using Microsoft.Extensions.Logging;
using Temporal.Curriculum.Writes.Domain.Orchestrations;
using Temporal.Curriculum.Writes.Messages.Commands;
using Temporal.Curriculum.Writes.Messages.Orchestrations;
using Temporalio.Activities;
using Temporalio.Api.Enums.V1;
using Temporalio.Client;
using Temporalio.Converters;
using Temporalio.Exceptions;
using Temporalio.Testing;
using Temporalio.Worker;
using Xunit.Abstractions;
using Errors = Temporal.Curriculum.Writes.Domain.Orchestrations.Errors;
using IntegrationErrors = Temporal.Curriculum.Writes.Domain.Integrations.Errors;

namespace Temporal.Curriculum.Writes.Tests.Orchestrations;

public class OnboardEntityTests : TestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    [Fact]
    public async Task ExecuteAsync_SimpleRun_GivenInvalidArgs_FailsFast()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();
        var wid = Guid.NewGuid();
        var emptyValue = "";
        var args = new OnboardEntityRequest(wid.ToString(), emptyValue,SkipApproval:true);
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
    public async Task ExecuteAsync_GivenHealthyService_RegistersCrmEntity()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();
        var args = new OnboardEntityRequest(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),SkipApproval:true);
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
    public async Task ExecuteAsync_GivenUnhealthyService_IsNotRetryable_FailsEntireOnboarding()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();
        var args = new OnboardEntityRequest(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), SkipApproval:true);
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
                errorType: IntegrationErrors.ErrServiceUnrecoverable,
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
            Assert.Equal(IntegrationErrors.ErrServiceUnrecoverable, appEx.ErrorType);
            
            // To get to the underlying Exception root cause, you must walk the BaseException .
            var baseEx = Assert.IsType<ApplicationFailureException>(e.GetBaseException());
            Assert.Equal(typeof(TaskCanceledException).Name, baseEx.ErrorType);
        });
        Assert.NotNull(requested);
        Assert.Equal(args.Id, requested.Id);
    }

    [Fact]
    public async Task ExecuteAsync_GivenOwnerApprovalNotMetTimely_NoDeputyOwner_ShouldCancelWithFailureOnboardingWithoutRegisteringEntity()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();
        var args = new OnboardEntityRequest(
            Id: Guid.NewGuid().ToString(), 
            Value: Guid.NewGuid().ToString(),
            DeputyOwnerEmail: null);
        
        RegisterCrmEntityRequest registrationRequestSent = null;
        RequestDeputyOwnerApprovalRequest deputyOwnerApprovalRequested = null;
        var workerOptions = new TemporalWorkerOptions("test");  
        workerOptions.LoggerFactory = LoggerFactory;
        workerOptions.AddWorkflow<OnboardEntity>(); 
        
        [Activity]
        Task RegisterCrmEntity(RegisterCrmEntityRequest req)
        {
            registrationRequestSent = req;
            throw new NotImplementedException();
        }

        [Activity]
        Task RequestDeputyOwnerApproval(RequestDeputyOwnerApprovalRequest req)
        {
            deputyOwnerApprovalRequested = req;
            throw new NotImplementedException();
        }
        workerOptions.AddActivity(RegisterCrmEntity);
        workerOptions.AddActivity(RequestDeputyOwnerApproval);

        using var worker = new TemporalWorker(
            env.Client,
            workerOptions
        );

        await worker.ExecuteAsync(async () =>
        {

            var handle = await env.Client.StartWorkflowAsync<OnboardEntity>(wf => wf.ExecuteAsync(args),
                new WorkflowOptions(args.Id, worker.Options.TaskQueue!));

            var e = await Assert.ThrowsAsync<WorkflowFailedException>(async () =>
            {
                await handle.GetResultAsync();
            });
            var appEx = Assert.IsType<ApplicationFailureException>(e.InnerException);
            Assert.Equal(Errors.ErrOnboardEntityTimedOut, appEx.ErrorType);
            Assert.Null(registrationRequestSent);
            Assert.Null(deputyOwnerApprovalRequested);
        });
    }
    
    [Fact]
    public async Task ExecuteAsync_GivenOwnerApprovalNotMetTimely_WithDeputyOwnerNotResponding_ShouldCancelWithFailureOnboardingWithoutRegisteringEntity()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();
        var args = new OnboardEntityRequest(
            Id: Guid.NewGuid().ToString(), 
            Value: Guid.NewGuid().ToString(),
            DeputyOwnerEmail: "deputy@dawg.com");
        
        RegisterCrmEntityRequest registrationRequestSent = null;
        RequestDeputyOwnerApprovalRequest deputyOwnerApprovalRequested = null;
        var workerOptions = new TemporalWorkerOptions("test") { LoggerFactory = LoggerFactory };
        workerOptions.AddWorkflow<OnboardEntity>(); 
        
        [Activity]
        Task RegisterCrmEntity(RegisterCrmEntityRequest req)
        {
            registrationRequestSent = req;
            throw new NotImplementedException();
        }

        [Activity]
        Task RequestDeputyOwnerApproval(RequestDeputyOwnerApprovalRequest req)
        {
            deputyOwnerApprovalRequested = req;
            return Task.CompletedTask;
        }
        workerOptions.AddActivity(RegisterCrmEntity);
        workerOptions.AddActivity(RequestDeputyOwnerApproval);

        using var worker = new TemporalWorker(
            env.Client,
            workerOptions
        );

        await worker.ExecuteAsync(async () =>
        {
            var handle = await env.Client.StartWorkflowAsync<OnboardEntity>(wf => wf.ExecuteAsync(args),
                new WorkflowOptions(args.Id, worker.Options.TaskQueue!));

            var e = await Assert.ThrowsAsync<WorkflowContinuedAsNewException>(async () =>
            {
                await handle.GetResultAsync(followRuns:false);
            });
            
            var canHandle = env.Client.GetWorkflowHandle<OnboardEntity>(handle.Id, e.NewRunId);

            var canInput = canHandle.FetchHistoryEventsAsync()
                .FirstAsync(e => e.EventType.Equals(EventType.WorkflowExecutionStarted))
                .Result
                .WorkflowExecutionStartedEventAttributes
                .Input
                .Payloads_
                .Select(p => new EncodedRawValue(env.Client.Options.DataConverter, p))
                .First()
                .ToValueAsync<OnboardEntityRequest>().Result;
            
            Assert.NotNull(canInput);
            Assert.Null(canInput.DeputyOwnerEmail);
            Assert.Null(registrationRequestSent); 
            Assert.NotNull(deputyOwnerApprovalRequested);
        });
    }
    [Fact]
    public async Task ApproveOnboardingEntity_GivenAwaitingApproval_ShouldRegisterEntityWithCrm()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();
        var args = new OnboardEntityRequest(
            Id: Guid.NewGuid().ToString(), 
            Value: Guid.NewGuid().ToString(),
            CompletionTimeoutSeconds:TimeSpan.FromSeconds(3).Seconds);
        
        
        RegisterCrmEntityRequest registrationRequestSent = null;
        RequestDeputyOwnerApprovalRequest deputyOwnerApprovalRequested = null;
        var workerOptions = new TemporalWorkerOptions("test") { LoggerFactory = LoggerFactory };
        workerOptions.AddWorkflow<OnboardEntity>(); 
        
        [Activity]
        Task RegisterCrmEntity(RegisterCrmEntityRequest req)
        {
            registrationRequestSent = req;
            return Task.CompletedTask;
        }

        [Activity]
        Task RequestDeputyOwnerApproval(RequestDeputyOwnerApprovalRequest req)
        {
            deputyOwnerApprovalRequested = req;
            return Task.CompletedTask;
        }
        workerOptions.AddActivity(RegisterCrmEntity);
        workerOptions.AddActivity(RequestDeputyOwnerApproval);

        using var worker = new TemporalWorker(
            env.Client,
            workerOptions
        );

        await worker.ExecuteAsync(async () =>
        {
            var handle = await env.Client.StartWorkflowAsync<OnboardEntity>(wf => wf.ExecuteAsync(args),
                new WorkflowOptions(args.Id, worker.Options.TaskQueue!));
            await env.DelayAsync(TimeSpan.FromSeconds(2));
            await handle.SignalAsync(wf => wf.ApproveAsync(new ApproveEntityRequest("beep")));
            await handle.GetResultAsync(followRuns:false);
            Assert.NotNull(registrationRequestSent); 
            Assert.Null(deputyOwnerApprovalRequested);
        });
    }
    [Fact]
    public async Task RejectOnboardingEntity_GivenAwaitingApproval_ShouldNotRegisterEntityWithCrm()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();
        var args = new OnboardEntityRequest(
            Id: Guid.NewGuid().ToString(), 
            Value: Guid.NewGuid().ToString(),
            CompletionTimeoutSeconds:TimeSpan.FromSeconds(3).Seconds);
        
        
        RegisterCrmEntityRequest registrationRequestSent = null;
        RequestDeputyOwnerApprovalRequest deputyOwnerApprovalRequested = null;
        var workerOptions = new TemporalWorkerOptions("test") { LoggerFactory = LoggerFactory };
        workerOptions.AddWorkflow<OnboardEntity>(); 
        
        [Activity]
        Task RegisterCrmEntity(RegisterCrmEntityRequest req)
        {
            registrationRequestSent = req;
            return Task.CompletedTask;
        }

        [Activity]
        Task RequestDeputyOwnerApproval(RequestDeputyOwnerApprovalRequest req)
        {
            deputyOwnerApprovalRequested = req;
            return Task.CompletedTask;
        }
        workerOptions.AddActivity(RegisterCrmEntity);
        workerOptions.AddActivity(RequestDeputyOwnerApproval);

        using var worker = new TemporalWorker(
            env.Client,
            workerOptions
        );

        await worker.ExecuteAsync(async () =>
        {
            var handle = await env.Client.StartWorkflowAsync<OnboardEntity>(wf => wf.ExecuteAsync(args),
                new WorkflowOptions(args.Id, worker.Options.TaskQueue!));
            await env.DelayAsync(TimeSpan.FromSeconds(2));
            await handle.SignalAsync(wf => wf.RejectAsync(new RejectEntityRequest("beep")));
            await handle.GetResultAsync(followRuns:false);
            Assert.Null(registrationRequestSent); 
            Assert.Null(deputyOwnerApprovalRequested);
        });
    }
    [Fact]
    public async Task SetValue_GivenPendingApproval_ShouldUpdateValue()
    {
        await using var env = await WorkflowEnvironment.StartLocalAsync();
        var args = new OnboardEntityRequest(
            Id: Guid.NewGuid().ToString(), 
            Value: Guid.NewGuid().ToString(),
            CompletionTimeoutSeconds:TimeSpan.FromSeconds(5).Seconds);
        
        
        RegisterCrmEntityRequest registrationRequestSent = null;
        RequestDeputyOwnerApprovalRequest deputyOwnerApprovalRequested = null;
        var workerOptions = new TemporalWorkerOptions("test") { LoggerFactory = LoggerFactory };
        workerOptions.AddWorkflow<OnboardEntity>(); 
        
        [Activity]
        Task RegisterCrmEntity(RegisterCrmEntityRequest req)
        {
            registrationRequestSent = req;
            return Task.CompletedTask;
        }

        [Activity]
        Task RequestDeputyOwnerApproval(RequestDeputyOwnerApprovalRequest req)
        {
            deputyOwnerApprovalRequested = req;
            return Task.CompletedTask;
        }
        workerOptions.AddActivity(RegisterCrmEntity);
        workerOptions.AddActivity(RequestDeputyOwnerApproval);

        using var worker = new TemporalWorker(
            env.Client,
            workerOptions
        );

        
        await worker.ExecuteAsync(async () =>
        {
            var handle = await env.Client.StartWorkflowAsync<OnboardEntity>(wf => wf.ExecuteAsync(args),
                new WorkflowOptions(args.Id, worker.Options.TaskQueue!));
            await env.DelayAsync(TimeSpan.FromSeconds(1));

            var actual = await handle.ExecuteUpdateAsync<OnboardEntity, OnboardEntityState>(wf =>
                wf.SetValueAsync(new SetValueRequest("boop")));
            Assert.Equal("boop", actual.CurrentValue);
            await handle.CancelAsync();
        });
        
        Assert.Null(registrationRequestSent); 
        Assert.Null(deputyOwnerApprovalRequested);
    }
    [Fact(Skip = "Update with test server is broken")]
    public async Task SetValue_GivenApprovedEntity_ShouldNotUpdateValue()
    {
        await using var env = await WorkflowEnvironment.StartLocalAsync();
        var args = new OnboardEntityRequest(
            Id: Guid.NewGuid().ToString(), 
            Value: Guid.NewGuid().ToString(),
            CompletionTimeoutSeconds:TimeSpan.FromSeconds(5).Seconds);
        
        
        RegisterCrmEntityRequest registrationRequestSent = null;
        RequestDeputyOwnerApprovalRequest deputyOwnerApprovalRequested = null;
        var workerOptions = new TemporalWorkerOptions("test") { LoggerFactory = LoggerFactory };
        workerOptions.AddWorkflow<OnboardEntity>(); 
        
        [Activity]
        Task RegisterCrmEntity(RegisterCrmEntityRequest req)
        {
            registrationRequestSent = req;
            return Task.CompletedTask;
        }

        [Activity]
        Task RequestDeputyOwnerApproval(RequestDeputyOwnerApprovalRequest req)
        {
            deputyOwnerApprovalRequested = req;
            return Task.CompletedTask;
        }
        workerOptions.AddActivity(RegisterCrmEntity);
        workerOptions.AddActivity(RequestDeputyOwnerApproval);

        using var worker = new TemporalWorker(
            env.Client,
            workerOptions
        );

        
        await worker.ExecuteAsync(async () =>
        {
            var handle = await env.Client.StartWorkflowAsync<OnboardEntity>(wf => wf.ExecuteAsync(args),
                new WorkflowOptions(args.Id, worker.Options.TaskQueue!));
            await env.DelayAsync(TimeSpan.FromSeconds(1));
            await handle.SignalAsync(wf => wf.ApproveAsync(new ApproveEntityRequest("")));
            try
            {
                var actual = await handle.ExecuteUpdateAsync<OnboardEntity, OnboardEntityState>(wf =>
                    wf.SetValueAsync(new SetValueRequest("boop")));
                Assert.Equal("boop", actual.CurrentValue);
            }
            catch (Exception e)
            {
                LoggerFactory.CreateLogger("update").LogInformation($"Got exception {e.GetType()}");
            }

            
            await handle.GetResultAsync();
        });
        
        Assert.Null(registrationRequestSent); 
        Assert.Null(deputyOwnerApprovalRequested);
    }
    public OnboardEntityTests(ITestOutputHelper output, ITestOutputHelper testOutputHelper) : base(output)
    {
        _testOutputHelper = testOutputHelper;
    }
}