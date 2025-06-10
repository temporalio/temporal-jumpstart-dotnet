using Onboardings.Domain.Clients.Email;
using Onboardings.Domain.Commands.V1;
using Temporalio.Activities;

namespace Onboardings.Domain.Notifications;

public class Handlers
{
    private IEmailClient _emailClient;

    public Handlers(IEmailClient emailClient)
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