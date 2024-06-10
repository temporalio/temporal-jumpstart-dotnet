using Temporal.Curriculum.Activities.Messages.Orchestrations;

namespace Temporal.Curriculum.Activities.Messages.API;

public class OnboardingsGet
{
    public string Id { get; set; }
    public string ExecutionStatus { get; set;  }
    public OnboardEntityRequest Input { get; set; }
}