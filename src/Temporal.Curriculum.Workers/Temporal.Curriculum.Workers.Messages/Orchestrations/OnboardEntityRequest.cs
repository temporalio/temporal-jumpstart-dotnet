using System.Diagnostics.CodeAnalysis;

namespace Temporal.Curriculum.Workers.Messages.Orchestrations;

public record OnboardEntityRequest(string Id, string Value);