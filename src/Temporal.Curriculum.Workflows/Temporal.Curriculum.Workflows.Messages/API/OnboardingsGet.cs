using Temporal.Curriculum.Workflows.Messages.Orchestrations;

namespace Temporal.Curriculum.Workflows.Messages.API;

public class OnboardingsGet
{
    public string Id { get; set; }
    public string ExecutionStatus { get; set;  }
    public OnboardEntityRequest Input { get; set; }
}