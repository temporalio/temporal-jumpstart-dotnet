using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Temporal.Curriculum.Timers.Domain.Integrations;
using Temporal.Curriculum.Timers.Messages.Commands;
using Temporal.Curriculum.Timers.Messages.Orchestrations;
using Temporalio.Activities;
using Temporalio.Api.Enums.V1;
using Temporalio.Common;
using Temporalio.Exceptions;
using Temporalio.Workflows;

using NotificationHandlers = Temporal.Curriculum.Timers.Domain.Notifications.Handlers;
namespace Temporal.Curriculum.Timers.Domain.Orchestrations;

public static class Errors
{
    public const string ErrOnboardEntityTimedOut = "Entity onboarding timed out.";
}
// This interface is only used for Documentation purposes.
// It is not required for Temporal Workflow implementations.
public interface IOnboardEntity
{
    /* ExecuteAsync */
    /*
     * 1. Validate input params for format
     * 2. Validate the args. Id must be unique in Application. Value must be alphanumeric and non-empty.
     * 3. Support Query for OnboardingState
     * 4. Execute Activity that writes to EntityStorage
     * 5. Await approval via `Approval` Signal for maximum of 7 days.
     *     i. If Approval.Rejected OR Approval period expires, then Cancel Onboarding; Compensate by DELETEing Entity in storage
     *     ii. If Approval.Approved then proceed with Onboarding
     * 6. Execute RegisterCRMEntity Activity
     *     i. If RegisterCRMEntity cannot succeed, Compensate
     */
    // ReSharper disable once UnusedMemberInSuper.Global
    Task ExecuteAsync(OnboardEntityRequest args);
}

public record OnboardEntityState(OnboardEntityRequest args, bool IsApproved = false);

[Workflow]
// ReSharper disable once ClassNeverInstantiated.Global
public class OnboardEntity : IOnboardEntity
{
    [WorkflowRun]
    public async Task ExecuteAsync(OnboardEntityRequest args)
    {
        var logger = Workflow.Logger;
        logger.LogInformation($"onboardingentity with runid {Workflow.Info.RunId}");
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

        var state = new OnboardEntityState(args);
        if (!args.SkipApproval)
        {

            var waitApprovalSecs = args.CompletionTimeoutSeconds;
            if (args.DeputyOwnerEmail != null)
            {
                // We lean into integer division here to be unconcerned about
                // determinism issues. Note that if we did this with a float/double
                // we could run into a problem with hardware results and violate the determinism
                // requirement for our Timer.
                waitApprovalSecs = args.CompletionTimeoutSeconds / 2;
            }

            // this blocks until we flip the `IsApproved` bit on our state object
            var conditionMet =
                await Workflow.WaitConditionAsync(() => state.IsApproved, TimeSpan.FromSeconds(waitApprovalSecs));
            if (!conditionMet)
            {
                if (args.DeputyOwnerEmail != null)
                {
                    // Since we are delivering an message, we want to restrict the number of retry attempts we make 
                    // lest we inadvertently build a SPAM server.
                    var notificationOptions =
                        new ActivityOptions() {
                            StartToCloseTimeout = TimeSpan.FromSeconds(60),
                            RetryPolicy = new RetryPolicy() { MaximumAttempts = 2, }
                        };
                    await Workflow.ExecuteActivityAsync((NotificationHandlers act) =>
                            act.RequestDeputyOwnerApproval(
                                new RequestDeputyOwnerApprovalRequest(args.Id, args.DeputyOwnerEmail!)),
                        notificationOptions);

                    // Now that we have notified the `DeputyOwner` that we need approval we can resume our wait for approval.
                    // Let's just recursively call our Workflow without the DeputyOwnerEmail specified and with the balance of our approval period.
                    var newArgs = args with {
                        DeputyOwnerEmail = null,
                        CompletionTimeoutSeconds = args.CompletionTimeoutSeconds - waitApprovalSecs
                    };
                    throw Workflow.CreateContinueAsNewException<OnboardEntity>(wf => wf.ExecuteAsync(newArgs),
                        new ContinueAsNewOptions() { TaskQueue = Workflow.Info.TaskQueue, });
                }

                var message = $"Onboarding {args.Id} failed to be approved in {args.CompletionTimeoutSeconds} seconds.";
                logger.LogError(message);
                // We never received approval from Deputy or primary owners, so we just fail the workflow
                throw new ApplicationFailureException(message, Errors.ErrOnboardEntityTimedOut);
            }
        }

        try
        {
            /*
             // During TDD for a Workflow definition it is handy to Execute the activity by its Name as seen here.
             // Now that we have implemented the Activity, though, we will replace it with the strongly typed invocation.
                await Workflow.ExecuteActivityAsync("RegisterCrmEntity", new []{new RegisterCrmEntityRequest(args.Id, args.Value)}, opts);
            */
            await Workflow.ExecuteActivityAsync((Handlers act) =>
                    act.RegisterCrmEntity(new RegisterCrmEntityRequest(args.Id, args.Value)),
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
    }

    private static void AssertValidRequest(OnboardEntityRequest args)
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
            throw new ApplicationFailureException("OnboardEntity.Id and OnboardEntity.Value is required");
        }

        if (args is { SkipApproval: true, DeputyOwnerEmail: not null })
        {
            throw new ApplicationFailureException("Either skip approval or provide a Deputy Owner email, not both.");
        }
        if(args.DeputyOwnerEmail != null && (TimeSpan.FromSeconds(args.CompletionTimeoutSeconds) < TimeSpan.FromDays(4)))
        {
            throw new ApplicationFailureException("Give at least four days to receive approval");
        }
    }
}