using Temporal.Curriculum.Reads.Domain.Clients.Email;
using Temporal.Curriculum.Reads.Messages.Commands;
using Temporalio.Activities;

namespace Temporal.Curriculum.Reads.Domain.Notifications;

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