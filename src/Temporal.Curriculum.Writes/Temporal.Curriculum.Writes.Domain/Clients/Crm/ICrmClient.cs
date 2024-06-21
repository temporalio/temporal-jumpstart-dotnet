namespace Temporal.Curriculum.Writes.Domain.Clients.Crm;

public class CrmEntityNotFoundException(string? message) : Exception(message);

public class CrmEntityExistsException(string? message) : Exception(message);

public interface ICrmClient
{
    Task RegisterCustomerAsync(string id, string value);
    Task<string> GetCustomerByIdAsync(string id);
}