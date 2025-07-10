using Onboardings.Domain.Clients.Email;
using Onboardings.Domain.Commands.V1;
using Temporalio.Activities;

namespace Onboardings.Domain.Workflows.OnboardEntity.Activities;

public class NotificationActivities
{
    private IEmailClient _emailClient;

    public NotificationActivities(IEmailClient emailClient)
    {
        _emailClient = emailClient;
    }

    [Activity]
    public async Task RequestDeputyOwnerApproval(RequestDeputyOwnerApprovalRequest args)
    {   
        await _emailClient.SendEmailAsync(args.DeputyOwnerEmail,
            body: $"Entity onboarding requests your approval for id {args.Id}");
    }
}