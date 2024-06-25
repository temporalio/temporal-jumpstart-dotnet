using Temporal.Jumpstart.App.Messages.Orchestrations;
using Temporal.Jumpstart.App.Messages.Queries;
using Temporalio.Workflows;

namespace Temporal.Jumpstart.App.Domain.Orchestrations;

[Workflow]
public class MyWorkflow
{
    
    [WorkflowQuery]
    public GetMyWorkflowStateResponse State { get; private set; }
    [WorkflowRun]
    public async Task ExecuteAsync(StartMyWorkflowRequest args)
    {
        State = new GetMyWorkflowStateResponse();
        throw new NotImplementedException();
    }
}