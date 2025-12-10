using System.Diagnostics;
using App.Api.Messages;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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

    [HttpPost("{id}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostUserAsync(string id, UserPost req)
    {
        var temporalClient = httpContextAccessor.HttpContext?.Features.GetRequiredFeature<ITemporalClient>();
        var opts = new WorkflowOptions { TaskQueue = temporalConfig.Value.Worker.TaskQueue, Id = id, };

        try
        {
            var handle = await temporalClient.StartWorkflowAsync<MyWorkflow>(wf => wf.ExecuteAsync(req.id), opts);
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
    public async Task<ActionResult<UserGet>> GetUserAsync(string id)
    {
        Debug.Assert(httpContextAccessor.HttpContext != null, "httpContextAccessor.HttpContext != null");
        var temporalClient = httpContextAccessor.HttpContext.Features.GetRequiredFeature<ITemporalClient>();

        try
        {
            var handle = temporalClient.GetWorkflowHandle<MyWorkflow>(id, null, null);
            var result = await handle.QueryAsync<string>(wf => wf.GetState());

            return Ok(new UserGet(result, result));
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