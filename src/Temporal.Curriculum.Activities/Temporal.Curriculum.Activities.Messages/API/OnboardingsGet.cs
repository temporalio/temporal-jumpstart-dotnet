using Temporal.Curriculum.Activities.Messages.Orchestrations;

namespace Temporal.Curriculum.Activities.Messages.API;

public record OnboardingsGet
{
    public string Id { get; set; }
    public string ExecutionStatus { get; set;  }
    public OnboardEntityRequest Input { get; set; }
}