using System.Linq.Expressions;
using System.Reflection;
using Moq;
using Temporalio.Api.Enums.V1;
using Temporalio.Client;
using Temporalio.Client.Interceptors;
using Temporalio.Client.Schedules;

namespace Onboardings.Api.Tests.Controllers;
public class CapturedWorkflowCall
{
    public Type WorkflowType { get; set; }
    public string MethodName { get; set; }
    public object[] Arguments { get; set; }
    public WorkflowOptions Options { get; set; }
}

public class ExpectQueryValue
{
    public object Value { get; set; }
    public string MethodName { get; set; }
}
public class MockTemporalClient : ITemporalClient
{
    private ClientOutboundInterceptor _interceptor;

    public void SetClientOutboundInterceptor(ClientOutboundInterceptor interceptor)
    {
        _interceptor = interceptor;
    }
    public List<CapturedWorkflowCall> CapturedCalls { get; set; } = new();
    public Dictionary<string, ExpectQueryValue> ExpectQueryValues { get; set; } = new();
    public TemporalClientOptions Options { get; }
    public IBridgeClientProvider BridgeClientProvider { get; }
    public AsyncActivityHandle GetAsyncActivityHandle(byte[] taskToken) => throw new NotImplementedException();

    public AsyncActivityHandle GetAsyncActivityHandle(string workflowId, string runId, string activityId) => throw new NotImplementedException();

    public Task<ScheduleHandle> CreateScheduleAsync(string scheduleId, Schedule schedule, ScheduleOptions? options = null) => throw new NotImplementedException();

    public ScheduleHandle GetScheduleHandle(string scheduleId) => throw new NotImplementedException();

    public IAsyncEnumerable<ScheduleListDescription> ListSchedulesAsync(ScheduleListOptions? options = null) => throw new NotImplementedException();

    public Task UpdateWorkerBuildIdCompatibilityAsync(string taskQueue, BuildIdOp buildIdOp, RpcOptions? rpcOptions = null) => throw new NotImplementedException();

    public Task<WorkerBuildIdVersionSets?> GetWorkerBuildIdCompatibilityAsync(string taskQueue, int maxSets = 0, RpcOptions? rpcOptions = null) => throw new NotImplementedException();

    public Task<WorkerTaskReachability> GetWorkerTaskReachabilityAsync(IReadOnlyCollection<string> buildIds, IReadOnlyCollection<string> taskQueues,
        TaskReachability? reachability = null, RpcOptions? rpcOptions = null) =>
        throw new NotImplementedException();

    public Task<WorkflowHandle<TWorkflow1, TResult>> StartWorkflowAsync<TWorkflow1, TResult>(Expression<Func<TWorkflow1, Task<TResult>>> workflowRunCall, WorkflowOptions options) => throw new NotImplementedException();


    public Task<WorkflowHandle> StartWorkflowAsync(string workflow, IReadOnlyCollection<object?> args, WorkflowOptions options) => throw new NotImplementedException();

    public WorkflowHandle GetWorkflowHandle(string id, string? runId = null, string? firstExecutionRunId = null) {
        return new WorkflowHandle(this, id);
    }

    public WorkflowHandle<TWorkflow> GetWorkflowHandle<TWorkflow>(string id, string? runId = null, string? firstExecutionRunId = null)
    { 
        return new WorkflowHandle<TWorkflow>(this, id);
    }

    public WorkflowHandle<TWorkflow, TResult> GetWorkflowHandle<TWorkflow, TResult>(string id, string? runId = null,
        string? firstExecutionRunId = null)
    {
            return new WorkflowHandle<TWorkflow, TResult>(this, id);
    }

    public Task<WorkflowUpdateHandle> StartUpdateWithStartWorkflowAsync<TWorkflow>(Expression<Func<TWorkflow, Task>> updateCall, WorkflowStartUpdateWithStartOptions options) => throw new NotImplementedException();

    public Task<WorkflowUpdateHandle<TUpdateResult>> StartUpdateWithStartWorkflowAsync<TWorkflow, TUpdateResult>(Expression<Func<TWorkflow, Task<TUpdateResult>>> updateCall,
        WorkflowStartUpdateWithStartOptions options) =>
        throw new NotImplementedException();

    public Task<WorkflowUpdateHandle> StartUpdateWithStartWorkflowAsync(string update, IReadOnlyCollection<object?> args,
        WorkflowStartUpdateWithStartOptions options) =>
        throw new NotImplementedException();

    public Task<WorkflowUpdateHandle<TUpdateResult>> StartUpdateWithStartWorkflowAsync<TUpdateResult>(string update, IReadOnlyCollection<object?> args,
        WorkflowStartUpdateWithStartOptions options) =>
        throw new NotImplementedException();

    public IAsyncEnumerable<WorkflowExecution> ListWorkflowsAsync(string query, WorkflowListOptions? options = null) => throw new NotImplementedException();

    public Task<WorkflowExecutionCount> CountWorkflowsAsync(string query, WorkflowCountOptions? options = null) => throw new NotImplementedException();

    public ITemporalConnection Connection { get; }

    public ClientOutboundInterceptor OutboundInterceptor => _interceptor;

    public WorkflowService WorkflowService { get; }
    public OperatorService OperatorService { get; }
    
    public Task<WorkflowHandle<T>> StartWorkflowAsync<T>(
        Expression<Func<T, Task>> workflow, 
        WorkflowOptions options = null)
    {
        // Extract method name and arguments
        var callInfo = AnalyzeExpression(workflow);
        
        // Store the captured call
        CapturedCalls.Add(new CapturedWorkflowCall
        {
            WorkflowType = typeof(T),
            MethodName = callInfo.MethodName,
            Arguments = callInfo.Arguments,
            Options = options
        });
        
        // Create mock handle
        var handle = new WorkflowHandle<T>(this, options.Id, 
            "test",
            "run1",
            "run1");
        
        return Task.FromResult(handle);
    }
    
    private (string MethodName, object[] Arguments) AnalyzeExpression<T>(Expression<Func<T, Task>> expression)
    {
        if (expression.Body is MethodCallExpression methodCall)
        {
            var methodName = methodCall.Method.Name;
            var arguments = methodCall.Arguments.Select(ExtractValue).ToArray();
            
            return (methodName, arguments);
        }
        
        return (null, Array.Empty<object>());
    }
    private object ExtractValue(Expression expression)
    {
        switch (expression)
        {
            case ConstantExpression constant:
                return constant.Value;
                
            case MemberExpression member when member.Expression is ConstantExpression constant:
                var field = member.Member as FieldInfo;
                var property = member.Member as PropertyInfo;
                
                if (field != null)
                    return field.GetValue(constant.Value);
                if (property != null)
                    return property.GetValue(constant.Value);
                break;
                
            case MemberExpression member when member.Expression is MemberExpression nestedMember:
                var parentValue = ExtractValue(nestedMember);
                var memberInfo = member.Member;
                
                if (memberInfo is FieldInfo nestedField)
                    return nestedField.GetValue(parentValue);
                if (memberInfo is PropertyInfo nestedProperty)
                    return nestedProperty.GetValue(parentValue);
                break;
        }
        
        // Fallback: compile and execute the expression
        try
        {
            var lambda = Expression.Lambda(expression);
            var compiled = lambda.Compile();
            return compiled.DynamicInvoke();
        }
        catch
        {
            return null;
        }
    }

}