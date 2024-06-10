using Temporal.Curriculum.Activities.Domain.Clients;
using Temporal.Curriculum.Activities.Domain.Integrations;
using Temporal.Curriculum.Activities.Messages.Commands;
using Temporalio.Testing;
using Xunit.Abstractions;

namespace Temporal.Curriculum.Activities.Tests.Orchestrations;

public class MockCrmClient : ICrmClient
{
    public Dictionary<string, string> RegisteredCustomers;

    public MockCrmClient()
    {
        RegisteredCustomers = new Dictionary<string, string>();
    }

    public void RegisterCustomer(string id, string value)
    {
        RegisteredCustomers.Add(id, value);
    }   
}
public class RegisterCrmEntityTests: TestBase
{
    public RegisterCrmEntityTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task RunAsync_RegisterCrmEntity_Fails_ServiceDown()
    {
        var crmClient = new MockCrmClient();
        var args = new RegisterCrmEntityRequest(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "customer");
        var handlers = new Handlers(crmClient);
        var env = new ActivityEnvironment();
        await Assert.ThrowsAsync<ApplicationException>(async () =>
        {
            await env.RunAsync(() => handlers.RegisterCrmEntity(args));
        });
    }
}