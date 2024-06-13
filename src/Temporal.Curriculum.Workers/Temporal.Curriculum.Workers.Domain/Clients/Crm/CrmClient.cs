using System.Runtime.Serialization;

namespace Temporal.Curriculum.Workers.Domain.Clients.Crm;

public class CrmEntityNotFoundException: Exception {
    public CrmEntityNotFoundException()
    {
    }

    protected CrmEntityNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public CrmEntityNotFoundException(string? message) : base(message)
    {
    }

    public CrmEntityNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public class CrmEntityExistsException : Exception
{
    public CrmEntityExistsException()
    {
    }

    protected CrmEntityExistsException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public CrmEntityExistsException(string? message) : base(message)
    {
    }

    public CrmEntityExistsException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
public interface ICrmClient
{
    Task  RegisterCustomerAsync(string id, string value);
    Task<string> GetCustomerByIdAsync(string id);
}
public class CrmClient: ICrmClient
{
    public Task RegisterCustomerAsync(string id, string value)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetCustomerByIdAsync(string id)
    {
        throw new NotImplementedException();
    }
}