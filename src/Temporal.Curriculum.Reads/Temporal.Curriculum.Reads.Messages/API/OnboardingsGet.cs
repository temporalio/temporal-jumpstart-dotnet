using Temporal.Curriculum.Reads.Messages.Orchestrations;

namespace Temporal.Curriculum.Reads.Messages.API;

// ReSharper disable once NotAccessedPositionalProperty.Global
public record OnboardingsGet(string? Id = null, 
    string? ExecutionStatus = null, 
    OnboardEntityRequest? Input = null);