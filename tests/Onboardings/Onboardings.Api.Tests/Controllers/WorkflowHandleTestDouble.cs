using Temporalio.Client;

namespace Onboardings.Api.Tests.Controllers;

public record WorkflowHandleTestDouble<TWorkflow,TResult> : WorkflowHandle<TWorkflow,TResult>
{
    public WorkflowHandleTestDouble(ITemporalClient Client, string Id, string? RunId = null, string? ResultRunId = null, string? FirstExecutionRunId = null) : base(Client, Id, RunId, ResultRunId, FirstExecutionRunId)
    {
    }

    public override Task<TQueryResult> QueryAsync<TQueryResult>(string query, IReadOnlyCollection<object?> args, WorkflowQueryOptions? options = null)
    {
        return Task.FromResult<TQueryResult>(Result is TQueryResult ? (TQueryResult)Result : default);
    }
        

    public object Result { get; set; }
}