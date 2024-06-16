using Temporal.Curriculum.Workflows.Messages.Orchestrations;

namespace Temporal.Curriculum.Workflows.Messages.API;

public record OnboardingsGet(string? Id=null, string? ExecutionStatus=null, OnboardEntityRequest? Input=null);