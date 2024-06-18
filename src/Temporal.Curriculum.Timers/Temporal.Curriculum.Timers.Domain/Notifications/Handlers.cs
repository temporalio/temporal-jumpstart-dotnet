using Temporal.Curriculum.Timers.Messages.Commands;
using Temporalio.Activities;

namespace Temporal.Curriculum.Timers.Domain.Notifications;

public class Handlers
{
    [Activity]
    public Task RequestDeputyOwnerApproval(RequestDeputyOwnerApprovalRequest args)
    {
        return Task.CompletedTask;
    }
}