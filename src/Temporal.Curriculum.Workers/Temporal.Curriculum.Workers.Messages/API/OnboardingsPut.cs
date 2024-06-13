namespace Temporal.Curriculum.Workers.Messages.API;

public record OnboardingsPut
{
    public required string Value { get; set; }
}