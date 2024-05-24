using System.Diagnostics.CodeAnalysis;

namespace Temporal.Curriculum.Workflows.Messages.Orchestrations;

public class OnboardEntityRequest
{
    public OnboardEntityRequest()
    {
    }

    [SetsRequiredMembers]
    public OnboardEntityRequest(string value)
    {
        Value = value;
    }
public required string Id { get; set; }
    public required string Value { get; set; }
}