using Jumpstart.Domain.Onboardings.Workflows.V1;
using Temporalio.Activities;

namespace Onboardings.Domain.Workflows.OnboardEntity.Activities;


public class OnboardEntityActivities
{
    [Activity]
    public GetOnboardEntityExecutionOptionsResponse GetOnboardEntityExecutionOptions(GetOnboardEntityExecutionOptionsRequest cmd)
    {
        const ulong DefaultCompletionTimeoutSeconds =  7 * 86400;

        return new GetOnboardEntityExecutionOptionsResponse
        {
            
        };
    }
}