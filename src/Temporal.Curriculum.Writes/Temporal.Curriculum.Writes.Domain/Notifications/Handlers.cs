using Temporal.Curriculum.Writes.Domain.Clients.Email;
using Temporal.Curriculum.Writes.Messages.Commands;
using Temporalio.Activities;

namespace Temporal.Curriculum.Writes.Domain.Notifications;

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