using System.Collections.Concurrent;

namespace Temporal.Curriculum.Timers.Domain.Clients.Email;

public class InMemoryEmailClient : IEmailClient
{
    private List<Tuple<string, string>> _sentEmails = new List<Tuple<string, string>>();

    public Task SendEmailAsync(string email, string body)
    {
        _sentEmails.Add(new Tuple<string, string>(email, body));
        return Task.CompletedTask;
    }
}