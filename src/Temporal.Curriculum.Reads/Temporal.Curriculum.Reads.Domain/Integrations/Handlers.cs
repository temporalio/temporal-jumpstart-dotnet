using Microsoft.Extensions.Logging;
using Temporal.Curriculum.Reads.Domain.Clients.Crm;
using Temporal.Curriculum.Reads.Messages.Commands;
using Temporalio.Activities;
using Temporalio.Exceptions;

namespace Temporal.Curriculum.Reads.Domain.Integrations;

public static class Errors
{
    public const string ErrServiceUnrecoverable = "CRM Service is not recoverable";
}

// Handlers wraps underlying integrations clients to meet Workflow singular message contracts
// ReSharper disable once ClassNeverInstantiated.Global
public class Handlers(ICrmClient crmClient)
{
    [Activity]
    public async Task RegisterCrmEntity(RegisterCrmEntityRequest args)
    {
        var logger = ActivityExecutionContext.Current.Logger;
        try
        {
            // Idempotency check - does the Entity already exist?
            // If so, just return
            var existingEntityValue = await crmClient.GetCustomerByIdAsync(args.Id);
#pragma warning disable CA2254
            logger.LogInformation($"Entity {args.Id} already exists as {existingEntityValue}");
#pragma warning restore CA2254
        }
        catch (CrmEntityNotFoundException)
        {
            // Only register with CRM if the customer is not found.
            // Ideally, we would not use Exceptions for control flow but the underlying CRM has no way to "Check" 
            // for the customer .
            try
            {
                await crmClient.RegisterCustomerAsync(args.Id, args.Value);
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
                        ex.Message,
                        ex,
                        Errors.ErrServiceUnrecoverable,
                        true);
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