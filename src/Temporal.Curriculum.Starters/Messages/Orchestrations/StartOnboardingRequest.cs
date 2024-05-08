using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Temporal.Curriculum.Starters.Messages.Orchestrations;

public class StartOnboardingRequest
{
    public StartOnboardingRequest()
    {
    }

    [SetsRequiredMembers]
    public StartOnboardingRequest(string value)
    {
        Value = value;
    }

    public required string Value { get; set; }
}