using Temporal.Curriculum.Workflows.Messages.Orchestrations;
using Temporalio.Exceptions;
using Temporalio.Workflows;

namespace Temporal.Curriculum.Workflows.Domain.Orchestrations;

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
    private void validateRequest(OnboardEntityRequest args)
    {
        if (args.Id.Equals(""))
        {
            throw new ApplicationFailureException("OnboardEntity.Id is required");
        }
    }
    [WorkflowRun]
    public async Task ExecuteAsync(OnboardEntityRequest args)
    {
        
        await Workflow.DelayAsync(2000);
    }
}