using Temporal.Curriculum.Reads.Messages.Orchestrations;
using Temporal.Curriculum.Reads.Messages.Values;

namespace Temporal.Curriculum.Reads.Messages.API;

// ReSharper disable once NotAccessedPositionalProperty.Global
public record OnboardingsGet(string Id, 
    string CurrentValue,
    Approval Approval);