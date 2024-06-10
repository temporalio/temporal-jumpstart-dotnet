using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Temporal.Curriculum.Activities.Api.Channels;

[Route("api/onboardings")]
[ApiController]
public class OnboardingsController:ControllerBase  {
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<TemporalConfig> _temporalConfig;
    private readonly ILogger _logger;

    public OnboardingsController(IHttpContextAccessor httpContextAccessor, 
        IOptions<TemporalConfig> temporalConfig, ILoggerFactory logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _temporalConfig = temporalConfig;
        _logger = logger.CreateLogger<OnboardingsController>();
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartOnboardingAsync(string id, OnboardingsPut req)
    {
        var temporalClient = _httpContextAccessor.HttpContext?.Features.GetRequiredFeature<ITemporalClient>();
        
        var opts = new WorkflowOptions
        {
            // BestPractice: WorkflowIds should have business meaning.
            // Details: This identifier can be an AccountID, SessionID, etc.
            // 1. Prefer _pushing_ an WorkflowID down instead of retrieving after-the-fact.
            // 2. Acquaint your self with the "Workflow ID Reuse Policy" to fit your use case
            // Reference: https://docs.temporal.io/workflows#workflow-id-reuse-policy
            Id = id,
            TaskQueue = _temporalConfig.Value.Worker.TaskQueue,
            // BestPractice: Do not fail a workflow on intermittent (eg bug) errors; prefer handling failures at the Activity level within the Workflow.
            // Details: A Workflow will very rarely need one to specify a RetryPolicy when starting a Workflow and we strongly discourage it.
            // Only Exceptions that inherit from `FailureException` will cause a RetryPolicy to be enforced. Other Exceptions will cause the WorkflowTask
            // to be rescheduled so that Workflows can continue to make progress once repaired/redeployed with corrections.
            // Reference: https://github.com/temporalio/sdk-dotnet/?tab=readme-ov-file#workflow-exceptions
            RetryPolicy = null,
            IdReusePolicy = WorkflowIdReusePolicy.RejectDuplicate,
        };
        WorkflowHandle? handle = null;
        var alreadyStarted = false;
        try
        {
            handle = await temporalClient.StartWorkflowAsync((OnboardEntity wf) => wf.ExecuteAsync(new OnboardEntityRequest(id, req.Value)), opts);
        }
        catch (WorkflowAlreadyStartedException e)
        {
            alreadyStarted = true;
            _logger.LogError("workflow {id} already started {e}", id, e);
            // swallow this exception since this is an PUT (idempotent)
        }

        if (handle != null || alreadyStarted)
        {
            // poor man's uri template. prefer RFC 6570 implementation
            var location = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/onboardings/{id}";
            return Accepted( location);
        }

        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
    } 
    [HttpGet("{id}")]
    [Produces("application/json")]
    public async Task<ActionResult<OnboardingsGet>> GetOnboardingStatus(string id)
    {
        var result = new OnboardingsGet();
        var temporalClient = _httpContextAccessor.HttpContext.Features.GetRequiredFeature<ITemporalClient>();

        // this is relatively advanced use of the TemporalClient but is shown here to 
        // illustrate how to interact with the lower-level gRPC API for extracting details
        // about the WorkflowExecution. 
        // We will be replacing this usage with a `Query` invocation to be simpler and more explicit.
        // This module will not overly explain this interaction but will be valuable later when we
        // want to reason about our Executions with more detail.
        var handle = temporalClient.GetWorkflowHandle(id);
        result.Id = handle.Id;
        try
        {
            var describe = await handle.DescribeAsync();
            result.ExecutionStatus = describe.Status.ToString();
            var hist = await handle.FetchHistoryAsync();
            var started = hist.Events.First(e => e.EventType == EventType.WorkflowExecutionStarted);
            foreach (var payload in started.WorkflowExecutionStartedEventAttributes.Input.Payloads_)
            {
                result.Input =
                    (OnboardEntityRequest?)DataConverter.Default.PayloadConverter.ToValue(payload,
                        typeof(OnboardEntityRequest)) ?? throw new InvalidOperationException();
            }

            return Ok(result);
        }
        catch (RpcException e)
        {
            if(e.Code.Equals(RpcException.StatusCode.NotFound)) {
                return NotFound();
            }
        }
        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
    }
}