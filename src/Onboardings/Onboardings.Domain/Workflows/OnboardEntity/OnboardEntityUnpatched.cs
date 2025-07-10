using Microsoft.Extensions.Logging;
using Onboardings.Domain.Commands.V1;
using Onboardings.Domain.Queries.V2;
using Onboardings.Domain.Values.V1;
using Onboardings.Domain.Workflows.OnboardEntity.Activities;
using Onboardings.Domain.Workflows.V2;
using Temporalio.Api.Enums.V1;
using Temporalio.Common;
using Temporalio.Exceptions;
using Temporalio.Workflows;

namespace Onboardings.Domain.Workflows.OnboardEntity;


// This is one way to identify the Workflow for discovery
[Workflow("OnboardEntity")]
// ReSharper disable once ClassNeverInstantiated.Global
public class OnboardEntityUnpatched : IOnboardEntity
{
    private GetEntityOnboardingStateResponse _state;
    public static ulong DefaultCompletionTimeoutSeconds =  7 * 86400;
    
    [WorkflowRun]
    public async Task ExecuteAsync(OnboardEntityRequest args)
    {
        args = AssertValidRequest(args);
        _state = new GetEntityOnboardingStateResponse
        {
            Args = args,
            Id = args.Id,
            CurrentValue = args.Value,
            Approval = new Approval
            {
                Status = args.SkipApproval ? ApprovalStatus.Approved : ApprovalStatus.Pending
            }
        };
      

    var logger = Workflow.Logger;
        logger.LogInformation($"onboarding entity with runid {Workflow.Info.RunId}");
        AssertValidRequest(args);

        var opts = new ActivityOptions {
            StartToCloseTimeout = TimeSpan.FromSeconds(5),
            // Targeting a specific TaskQueue for Activities is useful if you have hosts that run expensive hardware, 
            // need rate limiting provided by the Temporal service, or access to resources at those hosts in isolation.
            // Prefer using TaskQueue assignment for strategic reasons; that is, split things up when you really need it.
            // The TaskQueue assignment done here is redundant since by default Activities will be executed that are subscribed
            // to the TaskQueue this Workflow execution is using. 
            TaskQueue = Workflow.Info.TaskQueue
        };

        if (!args.SkipApproval)
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
                opts);
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
        await Workflow.ExecuteActivityAsync("NotifyOnboardEntityCompleted", new string[]{args.Id},
            new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(10), });
    }

    private async Task AwaitApproval(OnboardEntityRequest args)
    {
        var logger = Workflow.Logger;
        var waitApprovalSecs = args.CompletionTimeoutSeconds;
        if (args.HasDeputyOwnerEmail)
        {
            // We lean into integer division here to be unconcerned about
            // determinism issues. Note that if we did this with a float/double
            // we could run into a problem with hardware results and violate the determinism
            // requirement for our Timer.
            waitApprovalSecs = args.CompletionTimeoutSeconds / 2;
        }
        logger.LogInformation($"Waiting {waitApprovalSecs} seconds for approval");

        // this blocks until we flip the `ApprovalStatus` bit on our state object
        var conditionMet =
            await Workflow.WaitConditionAsync(() => !_state.Approval.Status.Equals(ApprovalStatus.Pending), TimeSpan.FromSeconds(waitApprovalSecs));
        if (!conditionMet)
        {
            logger.LogInformation("entered failure to receive approval");
            if (!args.HasDeputyOwnerEmail)
            {
                var message = $"Onboarding {args.Id} failed to be approved in {args.CompletionTimeoutSeconds} seconds.";
                logger.LogError(message);
                // We never received approval from Deputy or primary owners, so we just fail the workflow
                throw new ApplicationFailureException(message, nameof(Values.V1.Errors.OnboardEntityTimedOut));
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
            // Let's just recursively call our Workflow without the DeputyOwnerEmail specified and with the balance of our approval period.
            var newArgs = new OnboardEntityRequest {
                Id = args.Id,
                Value = _state.CurrentValue,
                // DeputyOwnerEmail = null,
                CompletionTimeoutSeconds = args.CompletionTimeoutSeconds - waitApprovalSecs,
                Email = args.Email,
            };
            throw Workflow.CreateContinueAsNewException<OnboardEntity>(wf => wf.ExecuteAsync(newArgs),
                new ContinueAsNewOptions() { TaskQueue = Workflow.Info.TaskQueue, });
        }
    }

    private static OnboardEntityRequest AssertValidRequest(OnboardEntityRequest args)
    {
        if (string.IsNullOrEmpty(args.Id) || string.IsNullOrEmpty(args.Value))
            /*
             * Temporal is not prescriptive about the strategy you choose for indicating failures in your Workflows.
             *
             * We throw an ApplicationFailureException here which would ultimately result in a `WorkflowFailedException`.
             * This is a common way to fail a Workflow which will never succeed due to bad arguments or some other invariant.
             *
             * It is common to use ApplicationFailure for business failures, but these should be considered distinct from an intermittent failure such as
             * a bug in the code or some dependency which is temporarily unavailable. Temporal can often recover from these kinds of intermittent failures
             * with a redeployment, downstream service correction, etc. These intermittent failures would typically result in an Exception NOT descended from
             * TemporalFailure and would therefore NOT fail the Workflow Execution.
             *
             * If you have explicit business metrics setup to monitor failed Workflows, you could alternatively return a "Status" result with the business failure
             * and allow the Workflow Execution to "Complete" without failure.
             *
             * Note that `WorkflowFailedException` will count towards the `workflow_failed` SDK Metric (https://docs.temporal.io/references/sdk-metrics#workflow_failed).
             */
        {
            throw new ApplicationFailureException("OnboardEntity.Id and OnboardEntity.Value is required", nameof(Values.V1.Errors.InvalidArguments));
        }

        if (args is { SkipApproval: true, HasDeputyOwnerEmail: true })
        {
            throw new ApplicationFailureException("Either skip approval or provide a Deputy Owner email, not both.",nameof(Values.V1.Errors.InvalidArguments));
        }
        if(!string.IsNullOrEmpty(args.DeputyOwnerEmail) && (TimeSpan.FromSeconds(args.CompletionTimeoutSeconds) < TimeSpan.FromDays(4)))
        {
            throw new ApplicationFailureException("Give at least four days to receive approval",nameof(Values.V1.Errors.InvalidArguments));
        }
        if (args.CompletionTimeoutSeconds < 1)
        {
            args.CompletionTimeoutSeconds = DefaultCompletionTimeoutSeconds;
        }
        return args;
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