using Temporal.Curriculum.Starters.Messages.Orchestrations;

namespace Temporal.Curriculum.Starters.Messages.API;

public record OnboardingsGet(string Id, string ExecutionStatus, StartOnboardingRequest Input);