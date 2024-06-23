using System.Collections.Concurrent;
using System.Net;

namespace Temporal.Curriculum.Reads.Domain.Clients.Crm;

public class InMemoryCrmClient : ICrmClient
{
    private readonly ConcurrentDictionary<string, string> _database = new();

    public Task RegisterCustomerAsync(string id, string value)
    {
        if (_database.ContainsKey(id))
        {
            throw new CrmEntityExistsException($"Entity {id} already exists");
        }

        if (value.Contains("timeout"))
        {
            throw new TaskCanceledException($"Timeout spoofed for {id} because {value}", null,
                new CancellationToken(true));
        }

        if (!_database.TryAdd(id, value))
        {
            throw new HttpRequestException($"Failed to add {id}/{value}", null, HttpStatusCode.BadRequest);
        }

        return Task.CompletedTask;
    }

    public Task<string> GetCustomerByIdAsync(string id)
    {
        if (_database.TryGetValue(id, out var value))
        {
            return Task.FromResult(value);
        }

        if (id.Contains("timeout"))
        {
            throw new TaskCanceledException($"Timeout spoofed for {id}", null, new CancellationToken(true));
        }

        throw new CrmEntityNotFoundException($"Entity {id} not found");
    }
}