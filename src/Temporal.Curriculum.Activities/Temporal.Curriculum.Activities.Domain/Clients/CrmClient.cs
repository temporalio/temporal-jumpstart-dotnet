namespace Temporal.Curriculum.Activities.Domain.Clients;

public interface ICrmClient
{
    void RegisterCustomer(string id, string value);
}
public class CrmClient: ICrmClient
{
    public void RegisterCustomer(string id, string value)
    {
        throw new NotImplementedException();
    }
}