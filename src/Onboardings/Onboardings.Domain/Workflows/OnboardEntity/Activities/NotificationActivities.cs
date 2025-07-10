using Onboardings.Domain.Clients.Email;
using Onboardings.Domain.Commands.V1;
using Temporalio.Activities;

namespace Onboardings.Domain.Workflows.OnboardEntity.Activities;

public class NotificationActivities(IEmailClient emailClient)
{

    [Activity]
    public async Task RequestDeputyOwnerApproval(RequestDeputyOwnerApprovalRequest args)
    {   
        await emailClient.SendEmailAsync(args.DeputyOwnerEmail,
            body: $"Entity onboarding requests your approval for id {args.Id}");
    }
    [Activity]
    public void NotifyOnboardEntityCompleted(string id)
    {
        
    }
    
}