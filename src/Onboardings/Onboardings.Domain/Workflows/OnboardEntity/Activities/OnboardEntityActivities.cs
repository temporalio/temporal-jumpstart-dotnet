using System.Runtime.InteropServices.ComTypes;
using Jumpstart.Domain.Onboardings.Workflows.V1;
using Temporalio.Activities;

namespace Onboardings.Domain.Workflows.OnboardEntity.Activities;


public class OnboardEntityActivities
{
    [Activity]
    public GetOnboardEntityExecutionOptionsResponse GetOnboardEntityExecutionOptions(GetOnboardEntityExecutionOptionsRequest cmd)
    {
        var minDeputyApprovedTimeSpan = TimeSpan.FromDays(4).Seconds;
        
        var opt = cmd.Args.Options ?? new OnboardEntityExecutionOptions();
        
        // if there is not an ApprovalTimeout provided, use the Default value 
        if (opt.ApprovalTimeoutSeconds == 0)
        {
            ulong defaultApprovalTimeoutSeconds =  7 * 86400;
            // we can safely access the environment here and have the result written to workflow history
            var env = Environment.GetEnvironmentVariable(OnboardEntity.EnvironmentKeyDefaultApprovalTimeoutSeconds);
            if (!string.IsNullOrWhiteSpace(env))
            {
                defaultApprovalTimeoutSeconds = ulong.Parse(env);
            }
            opt.ApprovalTimeoutSeconds = defaultApprovalTimeoutSeconds;
        }
        
        if (cmd.Args.HasDeputyOwnerEmail)
        {
            // if the deputy owner email is provided , we want to wait HALF the amount of time specified before
            // giving deputy owner opportunity to Approve.
            // For this to make sense we don't let the approval time fall below a sane minimum 
            opt.ApprovalTimeoutSeconds = Math.Max((ulong)minDeputyApprovedTimeSpan, opt.ApprovalTimeoutSeconds / 2);
        }
       
        return new GetOnboardEntityExecutionOptionsResponse
        {
            Options =  opt,
        };
    }
}