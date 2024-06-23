using Temporal.Curriculum.Writes.Messages.Orchestrations;

namespace Temporal.Curriculum.Writes.Messages.API;

// ReSharper disable once NotAccessedPositionalProperty.Global
public record OnboardingsGet(string? Id = null, 
    string? ExecutionStatus = null, 
    OnboardEntityRequest? Input = null);