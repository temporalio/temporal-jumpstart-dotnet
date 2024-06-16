using Temporal.Curriculum.Workflows.Messages.Orchestrations;

namespace Temporal.Curriculum.Workflows.Messages.API;

public record OnboardingsGet(string Id, string ExecutionStatus, OnboardEntityRequest Input);