using System.ComponentModel.DataAnnotations;

namespace Temporal.Curriculum.Starters.Messages.API;

public record OnboardingsPut
{
    public required string Value { get; set; }
}