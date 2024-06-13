using Temporal.Curriculum.Workers.Messages.Orchestrations;

namespace Temporal.Curriculum.Workers.Messages.API;

public record OnboardingsGet
{
    public string Id { get; set; }
    public string ExecutionStatus { get; set;  }
    public OnboardEntityRequest Input { get; set; }
}