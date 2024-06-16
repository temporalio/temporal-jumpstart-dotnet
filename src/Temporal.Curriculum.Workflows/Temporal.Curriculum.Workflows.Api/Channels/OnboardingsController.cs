using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Temporal.Curriculum.Workflows.Domain.Clients;
using Temporal.Curriculum.Workflows.Domain.Orchestrations;
using Temporal.Curriculum.Workflows.Messages.API;
using Temporal.Curriculum.Workflows.Messages.Orchestrations;
using Temporalio.Api.Enums.V1;
using Temporalio.Client;
using Temporalio.Converters;
using Temporalio.Exceptions;

namespace Temporal.Curriculum.Workflows.Api.Channels;

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
}