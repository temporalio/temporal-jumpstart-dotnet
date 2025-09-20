using System.ComponentModel;
using Jumpstart.Domain.Onboardings.Queries.V1;
using Jumpstart.Domain.Onboardings.Values.V1;
using Jumpstart.Domain.Onboardings.Workflows.V1;
using Microsoft.Extensions.Logging;
using Onboardings.Domain.Workflows.OnboardEntity.Activities;
using Temporalio.Api.Enums.V1;
using Temporalio.Exceptions;
using Temporalio.Workflows;
using RetryPolicy = Temporalio.Common.RetryPolicy;

namespace Onboardings.Domain.Workflows.OnboardEntity;


// This is one way to identify the Workflow for discovery
[Workflow("OnboardEntity")]
// ReSharper disable once ClassNeverInstantiated.Global
public class OnboardEntity : IOnboardEntity
{
    
    private GetEntityOnboardingStateResponse _state;
    public static string EnvironmentKeyDefaultApprovalTimeoutSeconds =  "ONBOARD_ENVIRONMENT_APPROVAL_TIMEOUT_SECONDS";

    // Since Signals and Updates could be run before the `_state` it initialized 
    // (the WorkflowRun method has not been invoked yet) we want to assign the
    // _state to zero-value in the WorkflowInit to avoid Null reference exceptions.
    // See this doc for more details: https://docs.temporal.io/handling-messages#workflow-initializers
    [WorkflowInit]
    public OnboardEntity(OnboardEntityRequest args)
    {
        var opts = args.Options ?? new OnboardEntityExecutionOptions();
        _state = new GetEntityOnboardingStateResponse
        {
            Args = args,
            Id = args.Id,
            CurrentValue = args.Value,
            Options = opts,
            Approval = new Approval
            {
                Status = opts.SkipApproval ? ApprovalStatus.Approved : ApprovalStatus.Pending
            }
        };   
    }
    
    [WorkflowRun]
    public async Task ExecuteAsync(OnboardEntityRequest args)
    {
        var logger = Workflow.Logger;
        logger.LogInformation($"onboarding entity with runid {Workflow.Info.RunId}");

        // Right away, we evaluate input arguments _inside a LocalActivity_ to determine the workflow execution options.
        // Prefer interacting with environment or other config values inside an Activity instead of directly in a Workflow to avoid
        // NonDeterminism errors that can be caused by changing configuration on executions in progress.
        var configuredOpts =  await Workflow.ExecuteLocalActivityAsync((OnboardEntityActivities act) =>
            act.GetOnboardEntityExecutionOptions(new GetOnboardEntityExecutionOptionsRequest { Args = args, }),  new LocalActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(15) });
        
        _state.Options = configuredOpts.Options;
        
        // Validate our inputs now
        AssertValidRequest(args);
        
        
        if (!_state.Options.SkipApproval)
        {
            await AwaitApproval(args);
        }

        if (!_state.Approval.Status.Equals(ApprovalStatus.Approved))
        {
            logger.LogWarning($"Failed to gain approval for {args.Id}. Aborting request.");
            return;
        }
        
        try
        {
            /*
             // During TDD for a Workflow definition it is handy to Execute the activity by its Name as seen here.
             // Now that we have implemented the Activity, though, we will replace it with the strongly typed invocation.
                await Workflow.ExecuteActivityAsync("RegisterCrmEntity", new []{new RegisterCrmEntityRequest(args.Id, args.Value)}, opts);
            */
            await Workflow.ExecuteActivityAsync((RegistrationActivities act) =>
                    act.RegisterCrmEntity(new RegisterCrmEntityRequest { Id = args.Id, Value=args.Value}),
                new ActivityOptions {
                    StartToCloseTimeout = TimeSpan.FromSeconds(5),
                    // Targeting a specific TaskQueue for Activities is useful if you have hosts that run expensive hardware, 
                    // need rate limiting provided by the Temporal service, or access to resources at those hosts in isolation.
                    // Prefer using TaskQueue assignment for strategic reasons; that is, split things up when you really need it.
                    // The TaskQueue assignment done here is redundant since by default Activities will be executed that are subscribed
                    // to the TaskQueue this Workflow execution is using. 
                    TaskQueue = Workflow.Info.TaskQueue
                });
        }
        catch (ActivityFailureException e)
        {
            logger.LogError(e.InnerException, "this is the Inner");
            if (e.RetryState == RetryState.NonRetryableFailure)
            {
                logger.LogError(
                    $"NonRetryable failure: {((ApplicationFailureException)e.GetBaseException()).ErrorType}");
            }

            throw;
        }
        
    }

    private async Task AwaitApproval(OnboardEntityRequest args)
    {
        var logger = Workflow.Logger;
        
        logger.LogInformation($"Waiting {_state.Options.ApprovalTimeoutSeconds} seconds for approval");

        // this blocks until we flip the `ApprovalStatus` bit on our state object
        var conditionMet =
            await Workflow.WaitConditionWithOptionsAsync(
                new WaitConditionOptions
                {
                    ConditionCheck = () => !_state.Approval.Status.Equals(ApprovalStatus.Pending),
                    Timeout = TimeSpan.FromSeconds(_state.Options.ApprovalTimeoutSeconds),
                    TimeoutSummary = "AwaitApproval",
                });
        if (!conditionMet)
        {
            logger.LogInformation("entered failure to receive approval");
            if (!args.HasDeputyOwnerEmail)
            {
                var message = $"Onboarding {args.Id} failed to be approved in {_state.Options.ApprovalTimeoutSeconds} seconds.";
                logger.LogError(message);
                // We never received approval from Deputy or primary owners, so we just fail the workflow
                throw new ApplicationFailureException(message, nameof(Errors.OnboardEntityTimedOut));
            }
              
            // Since we are delivering an message, we want to restrict the number of retry attempts we make 
            // lest we inadvertently build a SPAM server.
            var notificationOptions =
                new ActivityOptions() {
                    StartToCloseTimeout = TimeSpan.FromSeconds(60),
                    RetryPolicy = new RetryPolicy() { MaximumAttempts = 2, }
                };
            await Workflow.ExecuteActivityAsync((Activities.NotificationActivities act) =>
                    act.RequestDeputyOwnerApproval(
                        new RequestDeputyOwnerApprovalRequest { Id=args.Id, DeputyOwnerEmail = args.DeputyOwnerEmail! }),
                notificationOptions);

            // Now that we have notified the `DeputyOwner` that we need approval we can resume our wait for approval.
            // Let's just recursively call our Workflow without the DeputyOwnerEmail specified.
            var newArgs = new OnboardEntityRequest {
                Id = args.Id,
                Value = _state.CurrentValue,
                // DeputyOwnerEmail = null,
                Options = new OnboardEntityExecutionOptions{ 
                    ApprovalTimeoutSeconds = _state.Options.ApprovalTimeoutSeconds
                },
                Email = args.Email,
            };
            throw Workflow.CreateContinueAsNewException<OnboardEntity>(wf => wf.ExecuteAsync(newArgs),
                new ContinueAsNewOptions() { TaskQueue = Workflow.Info.TaskQueue, });
        }
        
    }

    // AssertValidRequest
    // Validates arguments for cases where an ApplicationFailure should result as a result of 
    // bad args. 
    private static void AssertValidRequest(OnboardEntityRequest args)
    {
        if (string.IsNullOrEmpty(args.Id) || string.IsNullOrEmpty(args.Value))
            /*
             * Temporal is not prescriptive about the strategy you choose for indicating failures in your Workflows.
             *
             * We throw an ApplicationFailureException here which would ultimately result in a `WorkflowFailedException`.
             * This is a common way to fail a Workflow which will never succeed due to bad arguments or some other invariant.
             *
             * It is common to use ApplicationFailure for business failures, but these should be considered distinct from a transient errors such as
             * a bug in the code or some dependency which is temporarily unavailable. Temporal can often recover from these kinds of transient errors
             * with a redeployment, downstream service correction, etc.
             * These transient failures would typically result in an Exception NOT descended from TemporalFailure and would therefore NOT fail the Workflow Execution.
             *
             * If you have explicit business metrics setup to monitor failed Workflows, you could alternatively return a "Status" result with the business failure
             * and allow the Workflow Execution to "Complete" without failure.
             *
             * Note that `WorkflowFailedException` will count towards the `workflow_failed` SDK Metric (https://docs.temporal.io/references/sdk-metrics#workflow_failed).
             */
        {
            throw new ApplicationFailureException("OnboardEntity.Id and OnboardEntity.Value is required", nameof(Errors.InvalidArguments));
        }

        if (args is { Options.SkipApproval: true, HasDeputyOwnerEmail: true })
        {
            throw new ApplicationFailureException("Either skip approval or provide a Deputy Owner email, not both.",nameof(Errors.InvalidArguments));
        }
    }

    [WorkflowSignal]
    public Task ApproveAsync(ApproveEntityRequest approveEntityRequest)
    {
        _state.Approval.Status = ApprovalStatus.Approved;
        _state.Approval.Comment = approveEntityRequest.Comment;
        return Task.CompletedTask;  
    }

    [WorkflowSignal]
    public Task RejectAsync(RejectEntityRequest rejectEntityRequest)
    {
        _state.Approval.Status = ApprovalStatus.Rejected;
        _state.Approval.Comment = rejectEntityRequest.Comment;
        return Task.CompletedTask;
    }

    [WorkflowUpdateValidator(nameof(SetValueAsync))]
    public void ValidateSetValue(SetValueRequest setValueRequest)
    {
        if (_state.Approval.Status != ApprovalStatus.Pending )
        {
            Workflow.Logger.LogWarning($"rejecting the value since Workflow is not pending approval");
            throw new InvalidOperationException("Only pending approval is allowed");
        }
    }
    [WorkflowUpdate]
    public Task<GetEntityOnboardingStateResponse> SetValueAsync(SetValueRequest cmd)
    {
        Workflow.Logger.LogInformation($"setting value from {_state.CurrentValue} to {cmd.Value}");
        // throw new ArgumentException("foo");
        _state.CurrentValue = cmd.Value;
        return Task.FromResult(_state);
    }
    [WorkflowQuery]
    public GetEntityOnboardingStateResponse GetEntityOnboardingStateAsync(GetEntityOnboardingStateRequest q)
    {
        return _state;
    }
    
}