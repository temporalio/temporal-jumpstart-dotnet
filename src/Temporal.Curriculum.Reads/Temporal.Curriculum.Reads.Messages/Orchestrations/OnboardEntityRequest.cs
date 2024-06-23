namespace Temporal.Curriculum.Reads.Messages.Orchestrations;

public record OnboardEntityRequest(
    string Id,
    string Value,
    int CompletionTimeoutSeconds = 7 * 86400,
    string? DeputyOwnerEmail = null, 
    bool SkipApproval = false);