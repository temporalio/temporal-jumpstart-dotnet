using Temporal.Curriculum.Activities.Messages.Orchestrations;

namespace Temporal.Curriculum.Activities.Messages.API;

public record OnboardingsGet(string Id, string ExecutionStatus, OnboardEntityRequest Input);