using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Temporal.Jumpstart.App.Domain.Clients.Temporal;
using Temporal.Jumpstart.App.Domain.Orchestrations;
using Temporal.Jumpstart.App.Messages.API;
using Temporal.Jumpstart.App.Messages.Orchestrations;
using Temporalio.Client;
using Temporalio.Exceptions;

namespace Temporal.Jumpstart.App.Api.Channels;

[Route("api/myresource")]
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
    public async Task<IActionResult> StartMyWorkflowAsync(string id, MyResourcePut req)
    {
        var temporalClient = httpContextAccessor.HttpContext?.Features.GetRequiredFeature<ITemporalClient>();

        var opts = new WorkflowOptions {
            Id = id,
            TaskQueue = temporalConfig.Value.Worker.TaskQueue,
        };
        WorkflowHandle? handle = null;
        var alreadyStarted = false;
        try
        {
            var args = new StartMyWorkflowRequest();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            handle = await temporalClient.StartWorkflowAsync(
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                (MyWorkflow wf) => wf.ExecuteAsync(args), opts);
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
        var location = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/api/onboardings/{id}";
       return Accepted(location);
    }

    [HttpGet("{id}")]
    [Produces("application/json")]
    public async Task<ActionResult<MyResourceGet>> GetMyWorkflowStateAsync(string id)
    {
        Debug.Assert(httpContextAccessor.HttpContext != null, "httpContextAccessor.HttpContext != null");
        var temporalClient = httpContextAccessor.HttpContext.Features.GetRequiredFeature<ITemporalClient>();
       
        try
        {
            var handle = temporalClient.GetWorkflowHandle<MyWorkflow>(id);
            var q = await handle.QueryAsync(wf =>  wf.State);
            var result = new MyResourceGet();
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