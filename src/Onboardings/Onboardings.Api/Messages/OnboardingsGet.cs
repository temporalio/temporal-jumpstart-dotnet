
using Onboardings.Domain.Messages.Orchestrations;

namespace Onboardings.Api.Messages;

// ReSharper disable once NotAccessedPositionalProperty.Global
public record OnboardingsGet(string? Id = null, 
    string? ExecutionStatus = null, 
    OnboardEntityRequest? Input = null);