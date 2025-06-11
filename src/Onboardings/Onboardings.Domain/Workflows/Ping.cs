using Microsoft.Extensions.Logging;
using Temporalio.Workflows;

namespace Onboardings.Domain.Workflows;

[Workflow]
public class Ping
{
    private string _state;

    [WorkflowRun]
    public Task<string> ExecuteAsync(string args)
    {
        Workflow.Logger.LogInformation($"Ping {args}");
        _state = $"pong: {args}";
        return Task.FromResult(_state);
    }

    [WorkflowQuery]
    public string GetState()
    {
        return _state;    
    }
}