using Temporal.Curriculum.Workflows.Messages.Orchestrations;

namespace Temporal.Curriculum.Workflows.Messages.API;

public record OnboardingsGet
{
    public string Id { get; set; }
    public string ExecutionStatus { get; set;  }
    public OnboardEntityRequest Input { get; set; }
}