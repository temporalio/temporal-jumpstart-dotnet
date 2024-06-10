using Temporal.Curriculum.Activities.Domain.Clients;
using Temporal.Curriculum.Activities.Messages.Commands;
using Temporalio.Activities;

namespace Temporal.Curriculum.Activities.Domain.Integrations;

// Handlers wraps underlying integrations clients to meet Workflow singular message contracts
public class Handlers
{
    private readonly ICrmClient _crmClient;

    public Handlers(ICrmClient crmClient)
    {
        _crmClient = crmClient;
    }

    public async Task RegisterCrmEntity(RegisterCrmEntityRequest args)
    {
        var logger = ActivityExecutionContext.Current.Logger;
        try
        {
            // idempotency check - does the Entity already exist?
            // if so, just return
            var existingEntityValue = await _crmClient.GetCustomerByIdAsync(args.Id);
            logger.LogInformation($"Entity {args.Id} already exists as {existingEntityValue}");
        }
        catch (CrmEntityNotFoundException)
        {
            // only register with CRM if the customer is not found
            // ideally, we would not use Exceptions for control flow but the underlying CRM has no way to "Check" 
            // for the customer 
            try
            {
                await _crmClient.RegisterCustomerAsync(args.Id, args.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw;
            }
        }
    }
}