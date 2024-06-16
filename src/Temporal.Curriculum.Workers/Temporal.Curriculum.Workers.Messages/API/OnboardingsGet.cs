using Temporal.Curriculum.Workers.Messages.Orchestrations;

namespace Temporal.Curriculum.Workers.Messages.API;

// ReSharper disable once NotAccessedPositionalProperty.Global
public record OnboardingsGet(string? Id = null, string? ExecutionStatus = null, OnboardEntityRequest? Input = null);