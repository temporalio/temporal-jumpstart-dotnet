using Temporal.Curriculum.Workflows.Messages.Orchestrations;
using Temporalio.Workflows;

namespace Temporal.Curriculum.Workflows.Domain.Orchestrations;

// This interface is only used for Documentation purposes.
// It is not required for Temporal Workflow implementations.
public interface IOnboardEntity
{
    /* ExecuteAsync */
    /*
     * 1. Validate input params for format
     * 2. Validate the args.
     */
    Task ExecuteAsync(OnboardEntityRequest args);
}

[Workflow]
public class OnboardEntity : IOnboardEntity
{
    [WorkflowRun]
    public async Task ExecuteAsync(OnboardEntityRequest args)
    {
        
        await Workflow.DelayAsync(2000);
    }
}