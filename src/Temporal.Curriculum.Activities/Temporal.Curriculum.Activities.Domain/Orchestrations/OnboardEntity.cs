using Temporal.Curriculum.Activities.Domain.Integrations;
using Temporal.Curriculum.Activities.Messages.Commands;
using Temporal.Curriculum.Activities.Messages.Orchestrations;
using Temporalio.Exceptions;
using Temporalio.Workflows;

namespace Temporal.Curriculum.Activities.Domain.Orchestrations;

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
    Task ExecuteAsync(OnboardEntityRequest args);
}

[Workflow]
public class OnboardEntity : IOnboardEntity
{
    private void AssertValidRequest(OnboardEntityRequest args)
    {
        if (string.IsNullOrEmpty(args.Id) || string.IsNullOrEmpty(args.Value) )
        {
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
            throw new ApplicationFailureException("OnboardEntity.Id and OnboardEntity.Value is required");
        }
    }

    [WorkflowRun]
    public async Task ExecuteAsync(OnboardEntityRequest args)
    {
        AssertValidRequest(args);
        
        var opts = new ActivityOptions()
        {
            StartToCloseTimeout = TimeSpan.FromSeconds(5),
        };

        /*
         // During TDD for a Workflow definition it is handy to Execute the activity by its Name as seen here.
         // Now that we have implemented the Activity, though, we will replace it with the strongly typed invocation.
            await Workflow.ExecuteActivityAsync("RegisterCrmEntity", new []{new RegisterCrmEntityRequest(args.Id, args.Value)}, opts);
        */
        await Workflow.ExecuteActivityAsync((Handlers act) => 
            act.RegisterCrmEntity(new(args.Id, args.Value)),
            opts);
        // ignore. more business logic to come
        await Workflow.DelayAsync(10000);
    }
}