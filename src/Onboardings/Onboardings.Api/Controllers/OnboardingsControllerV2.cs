using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Onboardings.Api.Messages;
using Onboardings.Api.V1;
using Onboardings.Domain.Clients.Temporal;
using Onboardings.Domain.Commands.V1;
using Onboardings.Domain.Queries.V2;
using Onboardings.Domain.Values.V1;
using Onboardings.Domain.Workflows;
using Onboardings.Domain.Workflows.V2;
using Temporalio.Api.Enums.V1;
using Temporalio.Client;
using Temporalio.Converters;
using Temporalio.Exceptions;

namespace Onboardings.Api.Controllers;

[Route("api/v2/onboardings")]
[ApiController]
public class OnboardingsControllerV2(
    IHttpContextAccessor httpContextAccessor,
    IOptions<TemporalConfig> temporalConfig,
    ILoggerFactory logger)
    : ControllerBase
{
    private readonly ILogger _logger = logger.CreateLogger<OnboardingsControllerV2>();

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OnboardEntityAsync(string id, OnboardingsPut req)
    {
        var temporalClient = httpContextAccessor.HttpContext?.Features.GetRequiredFeature<ITemporalClient>();

        if (req.Approval.Status.Equals(ApprovalStatus.Pending))
        {
            return await StartWorkflow(id, req, temporalClient);
        }

        try
        {
            // do we want to ignore the `value` ? 
            // TODO: Introduce the call to `SetValue` in Workflow with Update when improved 
            WorkflowHandle? handle = temporalClient.GetWorkflowHandle<OnboardEntity>(id);
            Expression<Func<OnboardEntity, Task>> signalCall = null;
            switch (req.Approval.Status)
            {
                case ApprovalStatus.Approved:
                    signalCall = wf => wf.ApproveAsync(new ApproveEntityRequest { Comment = req.Approval.Comment});
                    break;
                case ApprovalStatus.Rejected:
                    signalCall = wf => wf.RejectAsync(new RejectEntityRequest{Comment = req.Approval.Comment});
                    break;
                default:
                    return BadRequest();
            }
             await handle.SignalAsync<OnboardEntity>(signalCall);
             // poor man's uri template. prefer RFC 6570 implementation
             var location = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/v2/onboardings/{id}";
             return Accepted(location);
        } catch (RpcException e)
        {
            if (e.Code.Equals(RpcException.StatusCode.NotFound))
            {
                return NotFound();
            }
           
        }
        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
    }

    private async Task<IActionResult> StartWorkflow(string id, OnboardingsPut req, ITemporalClient? temporalClient)
    {
        var opts = new WorkflowOptions {
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
            var args = new OnboardEntityRequest { Id = id, Value = req.Value };
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            handle = await temporalClient.StartWorkflowAsync(
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                (OnboardEntity wf) => wf.ExecuteAsync(args), opts);
        }
        catch (WorkflowAlreadyStartedException e)
        {
            alreadyStarted = true;
            _logger.LogError("workflow {id} already started {e}", id, e);
            // swallow this exception since this is an PUT (idempotent)
            // consider doing a redirect to the resource at GET /api/onboardings/{id}
        }

        if (handle == null && !alreadyStarted)
        {
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
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

        try
        {
            var handle = temporalClient.GetWorkflowHandle<OnboardEntity>(id);
            var q = await handle.QueryAsync<GetEntityOnboardingStateResponse>(wf =>  
                wf.GetEntityOnboardingStateAsync(new ()));
            return Ok(new OnboardingsGet { Id = q.Id, CurrentValue = q.CurrentValue, Approval = q.Approval, });
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