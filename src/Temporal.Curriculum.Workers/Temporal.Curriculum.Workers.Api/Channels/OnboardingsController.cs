using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Temporal.Curriculum.Workers.Domain.Clients.Temporal;
using Temporal.Curriculum.Workers.Domain.Orchestrations;
using Temporal.Curriculum.Workers.Messages.API;
using Temporal.Curriculum.Workers.Messages.Orchestrations;
using Temporalio.Api.Enums.V1;
using Temporalio.Client;
using Temporalio.Converters;
using Temporalio.Exceptions;

namespace Temporal.Curriculum.Workers.Api.Channels;

[Route("api/onboardings")]
[ApiController]
public class OnboardingsController(
    IHttpContextAccessor httpContextAccessor,
    IOptions<TemporalConfig> temporalConfig,
    ILoggerFactory logger)
    : ControllerBase
{
    private readonly ILogger _logger = logger.CreateLogger<OnboardingsController>();

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OnboardEntityAsync(string id, OnboardingsPut req)
    {
        var temporalClient = httpContextAccessor.HttpContext?.Features.GetRequiredFeature<ITemporalClient>();

        var opts = new WorkflowOptions
        {
            // BestPractice: WorkflowIds should have business meaning.
            // Details: This identifier can be an AccountID, SessionID, etc.
            // 1. Prefer _pushing_ an WorkflowID down instead of retrieving after-the-fact.
            // 2. Acquaint your self with the "Workflow ID Reuse Policy" to fit your use case
            // Reference: https://docs.temporal.io/workflows#workflow-id-reuse-policy
            Id = id,
            TaskQueue = temporalConfig.Value.Worker.TaskQueue,
            // BestPractice: Do not fail a workflow on intermittent (eg bug) errors; prefer handling failures at the Activity level within the Workflow.
            // Details: A Workflow will very rarely need one to specify a RetryPolicy when starting a Workflow and we strongly discourage it.
            // Only Exceptions that inherit from `FailureException` will cause a RetryPolicy to be enforced. Other Exceptions will cause the WorkflowTask
            // to be rescheduled so that Workflows can continue to make progress once repaired/redeployed with corrections.
            // Reference: https://github.com/temporalio/sdk-dotnet/?tab=readme-ov-file#workflow-exceptions
            RetryPolicy = null,
            // Our requirements state that we want to allow the same WorkflowID if prior attempts were Canceled.
            // Therefore, we are using this Policy that will reject duplicates unless previous attempts did not reach terminal state as `Completed'.
            IdReusePolicy = WorkflowIdReusePolicy.AllowDuplicateFailedOnly,
        };
        WorkflowHandle? handle = null;
        var alreadyStarted = false;
        try
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            handle = await temporalClient.StartWorkflowAsync(
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                (OnboardEntity wf) => wf.ExecuteAsync(new OnboardEntityRequest(id, req.Value)), opts);
        }
        catch (WorkflowAlreadyStartedException e)
        {
            alreadyStarted = true;
            _logger.LogError("workflow {id} already started {e}", id, e);
            // swallow this exception since this is an PUT (idempotent)
        }

        if (handle == null && !alreadyStarted)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        // poor man's uri template. prefer RFC 6570 implementation
        var location = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/onboardings/{id}";
        return Accepted(location);

    }

    [HttpGet("{id}")]
    [Produces("application/json")]
    public async Task<ActionResult<OnboardingsGet>> GetOnboardingStatus(string id)
    {
        Debug.Assert(httpContextAccessor.HttpContext != null, "httpContextAccessor.HttpContext != null");
        var temporalClient = httpContextAccessor.HttpContext.Features.GetRequiredFeature<ITemporalClient>();

        // this is relatively advanced use of the TemporalClient but is shown here to 
        // illustrate how to interact with the lower-level gRPC API for extracting details
        // about the WorkflowExecution. 
        // We will be replacing this usage with a `Query` invocation to be simpler and more explicit.
        // This module will not overly explain this interaction but will be valuable later when we
        // want to reason about our Executions with more detail.
        var handle = temporalClient.GetWorkflowHandle(id);
        var result = new OnboardingsGet
        {
            Id = handle.Id
        };
        try
        {
            var describe = await handle.DescribeAsync();
            result = result with { ExecutionStatus = describe.Status.ToString() };
            var hist = await handle.FetchHistoryAsync();
            var started = hist.Events.First(e => e.EventType == EventType.WorkflowExecutionStarted);
            result = started.WorkflowExecutionStartedEventAttributes.Input.Payloads_.Aggregate(result,
                (current, payload) => current with
                {
                    Input = (OnboardEntityRequest?)DataConverter.Default.PayloadConverter.ToValue(payload,
                        typeof(OnboardEntityRequest)) ?? throw new InvalidOperationException()
                });

            return Ok(result);
        }
        catch (RpcException e)
        {
            if (e.Code.Equals(RpcException.StatusCode.NotFound))
            {
                return NotFound();
            }
        }

        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
    }
}