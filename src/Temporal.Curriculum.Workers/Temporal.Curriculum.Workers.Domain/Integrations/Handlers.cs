using Microsoft.Extensions.Logging;
using Temporal.Curriculum.Workers.Domain.Clients.Crm;
using Temporal.Curriculum.Workers.Messages.Commands;
using Temporalio.Activities;
using Temporalio.Exceptions;

namespace Temporal.Curriculum.Workers.Domain.Integrations;

public static class Errors {
    public const string ERR_SERVICE_UNRECOVERABLE = "CRM Service is not recoverable";
}

// Handlers wraps underlying integrations clients to meet Workflow singular message contracts
public class Handlers
{
    private readonly ICrmClient _crmClient;

    public Handlers(ICrmClient crmClient)
    {
        _crmClient = crmClient;
    }

    [Activity]
    public async Task RegisterCrmEntity(RegisterCrmEntityRequest args)
    {
        var logger = ActivityExecutionContext.Current.Logger;
        try
        {
            // Idempotency check - does the Entity already exist?
            // If so, just return
            var existingEntityValue = await _crmClient.GetCustomerByIdAsync(args.Id);
            logger.LogInformation($"Entity {args.Id} already exists as {existingEntityValue}");
        }
        catch (CrmEntityNotFoundException)
        {
            // Only register with CRM if the customer is not found.
            // Ideally, we would not use Exceptions for control flow but the underlying CRM has no way to "Check" 
            // for the customer .
            try
            {
                await _crmClient.RegisterCustomerAsync(args.Id, args.Value);
            }
            catch (TaskCanceledException ex)
            {
                // The API timed out and instead of allowing constant retries, we are deciding here
                // to disallow the Temporal retry facility.
                // This could be an intermittent failure though so are we sure we want to "give up" ? 
                // For our demonstration, we will show how to make the Activity tell the Workflow not to keep trying; 
                // but you may want to allow Temporal to keep retrying this API call.
                if (ex.CancellationToken.IsCancellationRequested)
                {
                    throw new ApplicationFailureException(
                        message: ex.Message,
                        inner: ex,
                        errorType: Errors.ERR_SERVICE_UNRECOVERABLE,
                        nonRetryable: true);
                }
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CRM Entity Registration failed");
                throw;
            }
        }
    }
}