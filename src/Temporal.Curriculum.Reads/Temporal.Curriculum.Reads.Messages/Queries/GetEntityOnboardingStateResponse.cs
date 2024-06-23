using Temporal.Curriculum.Reads.Messages.Values;

namespace Temporal.Curriculum.Reads.Messages.Queries;

public record GetEntityOnboardingStateRequest(string Id);
public record GetEntityOnboardingStateResponse(string Id, 
    string CurrentValue,
    Approval Approval);