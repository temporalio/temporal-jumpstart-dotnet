using Temporal.Curriculum.Timers.Messages.Orchestrations;

namespace Temporal.Curriculum.Timers.Messages.API;

// ReSharper disable once NotAccessedPositionalProperty.Global
public record OnboardingsGet(string? Id = null, 
    string? ExecutionStatus = null, 
    OnboardEntityRequest? Input = null);