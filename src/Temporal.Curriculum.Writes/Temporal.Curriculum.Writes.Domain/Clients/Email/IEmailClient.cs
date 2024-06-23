namespace Temporal.Curriculum.Writes.Domain.Clients.Email;

public interface IEmailClient
{
    Task SendEmailAsync(string email, string body);
}