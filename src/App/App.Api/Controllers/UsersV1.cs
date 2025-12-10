using System.Diagnostics;
using Jumpstart.Api.Onboardings.V1;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using App.Api.Messages;
using App.Domain.Clients.Temporal;
using App.Domain.Workflows;
using Temporalio.Client;
using Temporalio.Exceptions;

namespace App.Api.Controllers;


[Route("api/v1/users")]
[ApiController]
public class UsersControllerV1(
    IHttpContextAccessor httpContextAccessor,
    IOptions<TemporalConfig> temporalConfig,
    ILoggerFactory logger)
    : ControllerBase
{
    private readonly ILogger _logger = logger.CreateLogger<UsersControllerV1>();

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PutUserAsync(string id, PutPing req)
    {
        var temporalClient = httpContextAccessor.HttpContext?.Features.GetRequiredFeature<ITemporalClient>();
        var opts = new WorkflowOptions { TaskQueue = temporalConfig.Value.Worker.TaskQueue, Id = id, };

        try
        {
            var handle = await temporalClient.StartWorkflowAsync<MyWorkflow>(wf => wf.ExecuteAsync(req.Ping), opts);
            // poor man's uri template. prefer RFC 6570 implementation
            _logger.LogInformation("started workflow {id}", handle.Id);
            var location = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/v1/users/{id}";
            return Accepted(location);
        }
        catch (WorkflowAlreadyStartedException e)
        {
            return Conflict(e.Message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "failed to start workflow");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("{id}")]
    [Produces("application/json")]
    public async Task<ActionResult<OnboardingsGet>> GetUserAsync(string id)
    {
        Debug.Assert(httpContextAccessor.HttpContext != null, "httpContextAccessor.HttpContext != null");
        var temporalClient = httpContextAccessor.HttpContext.Features.GetRequiredFeature<ITemporalClient>();

        try
        {
            var handle = temporalClient.GetWorkflowHandle<MyWorkflow>(id, null, null);
            var result = await handle.QueryAsync<string>(wf => wf.GetState());

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