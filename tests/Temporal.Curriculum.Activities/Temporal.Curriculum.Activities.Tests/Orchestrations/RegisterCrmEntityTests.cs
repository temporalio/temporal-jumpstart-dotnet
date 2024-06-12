using Temporal.Curriculum.Activities.Domain.Clients;
using Temporal.Curriculum.Activities.Domain.Clients.Crm;
using Temporal.Curriculum.Activities.Domain.Integrations;
using Temporal.Curriculum.Activities.Messages.Commands;
using Temporalio.Testing;
using Xunit.Abstractions;

namespace Temporal.Curriculum.Activities.Tests.Orchestrations;

public class MockCrmClient : ICrmClient
{
    private readonly Exception? _exception;
    public IDictionary<string, string> RegisterCustomerCalls;
    public IDictionary<string, string> PreviouslyRegisteredEntities;
    public MockCrmClient(Exception ?exception)
    {
        _exception = exception;
        RegisterCustomerCalls = new Dictionary<string, string>();
        PreviouslyRegisteredEntities = new Dictionary<string, string>();
    }

    public async Task RegisterCustomerAsync(string id, string value)
    {
        RegisterCustomerCalls.Add(id, value);
        if (_exception != null)
        {
            throw _exception;
        }
    }

    public Task<string> GetCustomerByIdAsync(string id)
    {
        if (PreviouslyRegisteredEntities.ContainsKey(id))
        {
            return Task.FromResult(PreviouslyRegisteredEntities[id]);
        }

        throw new CrmEntityNotFoundException($"Entity {id} NotFound");
    }
}
public class RegisterCrmEntityTests(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public async Task RunAsync_RegisterCrmEntity_GivenNoPreviousFailure_FailsWithServiceDown()
    {
        var requestExc = new HttpRequestException("service is down");
        var crmClient = new MockCrmClient(requestExc);
        var args = new RegisterCrmEntityRequest(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        var handlers = new Handlers(crmClient);
        ActivityEnvironment env = new ActivityEnvironment()
        {
            Logger = LoggerFactory.CreateLogger("test"),
        };
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await env.RunAsync(() => handlers.RegisterCrmEntity(args));
        });
        
        Assert.Contains(args.Id, crmClient.RegisterCustomerCalls);
    }
    [Fact]
    public async Task RunAsync_RegisterCrmEntity_GivenRecordAlreadyExists_SucceedsWithoutMutation()
    {
        var args = new RegisterCrmEntityRequest(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var crmClient = new MockCrmClient(null);
        crmClient.PreviouslyRegisteredEntities.Add(args.Id, args.Value);
        var handlers = new Handlers(crmClient);
        ActivityEnvironment env = new ActivityEnvironment()
        {
            Logger = LoggerFactory.CreateLogger("test"),
        };
        await env.RunAsync(() => handlers.RegisterCrmEntity(args));
        Assert.Empty(crmClient.RegisterCustomerCalls);
    }
}