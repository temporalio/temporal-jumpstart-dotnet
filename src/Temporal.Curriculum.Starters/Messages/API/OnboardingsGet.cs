using Temporal.Curriculum.Starters.Messages.Orchestrations;

namespace Temporal.Curriculum.Starters.Messages.API;

public record OnboardingsGet(string? Id=null, string? ExecutionStatus=null, OnboardEntityRequest? Input=null);