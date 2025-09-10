using Jumpstart.Domain.Onboardings.Queries.V1;
using Jumpstart.Domain.Onboardings.Workflows.V1;

namespace Onboardings.Domain.Workflows.OnboardEntity;

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
    Task ApproveAsync(ApproveEntityRequest approveEntityRequest);
    Task RejectAsync(RejectEntityRequest rejectEntityRequest);
    Task<GetEntityOnboardingStateResponse> SetValueAsync(SetValueRequest cmd);
    GetEntityOnboardingStateResponse GetEntityOnboardingStateAsync(GetEntityOnboardingStateRequest q);

}
