using System.Diagnostics.CodeAnalysis;

namespace Temporal.Curriculum.Workflows.Messages.Orchestrations;

public record OnboardEntityRequest
{
    public OnboardEntityRequest()
    {
    }

    [SetsRequiredMembers]
    public OnboardEntityRequest(string id, string value)
    {
        Id = id;
        Value = value;
    }
    public required string Id { get; set; }
    public required string Value { get; set; }
}