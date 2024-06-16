using Temporal.Curriculum.Workers.Messages.Orchestrations;

namespace Temporal.Curriculum.Workers.Messages.API;

public record OnboardingsGet(string Id, string ExecutionStatus, OnboardEntityRequest Input);