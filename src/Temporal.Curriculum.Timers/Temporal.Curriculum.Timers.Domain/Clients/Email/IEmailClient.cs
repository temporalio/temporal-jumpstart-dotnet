namespace Temporal.Curriculum.Timers.Domain.Clients.Email;

public interface IEmailClient
{
    Task SendEmailAsync(string email, string body);
}