using Temporal.Curriculum.Starters.Messages.Orchestrations;

namespace Temporal.Curriculum.Starters.Messages.API;

public class OnboardingsGet
{
    public string Id { get; set; }
    public string ExecutionStatus { get; set;  }
    public StartOnboardingRequest Input { get; set; }
}