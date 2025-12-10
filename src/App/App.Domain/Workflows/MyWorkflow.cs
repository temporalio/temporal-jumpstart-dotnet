using Microsoft.Extensions.Logging;
using Temporalio.Workflows;

namespace App.Domain.Workflows;

[Workflow]
public class MyWorkflow
{
    private string _state;

    [WorkflowRun]
    public Task<string> ExecuteAsync(string args)
    {
        Workflow.Logger.LogInformation($"MyWorkflow {args}");
        _state = $"result: {args}";
        return Task.FromResult(_state);
    }

    [WorkflowQuery]
    public string GetState()
    {
        return _state;
    }
}