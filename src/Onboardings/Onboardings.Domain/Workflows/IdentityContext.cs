namespace Onboardings.Domain.Workflows;

public static class IdentityContext
{
    public static readonly AsyncLocal<Identity> User = new();
}