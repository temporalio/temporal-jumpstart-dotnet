using Temporal.Curriculum.Activities.Domain.Clients;
using Temporal.Curriculum.Activities.Messages.Commands;

namespace Temporal.Curriculum.Activities.Domain.Integrations;

public class Handlers
{
    private readonly ICrmClient _crmClient;

    public Handlers(ICrmClient crmClient)
    {
        _crmClient = crmClient;
    }

    public Task RegisterCrmEntity(RegisterCrmEntityRequest args)
    {
        throw new NotImplementedException();
    }
}