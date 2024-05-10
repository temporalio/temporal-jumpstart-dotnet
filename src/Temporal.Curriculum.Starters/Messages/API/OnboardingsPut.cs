using System.ComponentModel.DataAnnotations;

namespace Temporal.Curriculum.Starters.Messages.API;

public class OnboardingsPut
{
    public required string Value { get; set; }
}