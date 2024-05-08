using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Temporal.Curriculum.Starters.Messages.API;
using Temporal.Curriculum.Starters.Messages.Orchestrations;
using Temporalio.Client;

namespace Temporal.Curriculum.Starters.Channels;

[Route("api/onboardings")]
[ApiController]
public class OnboardingsController:ControllerBase  {
    private readonly ITemporalClient _temporalClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OnboardingsController(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPut()]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartOnboardingAsync(OnboardingsPost req)
    {
        // Decouple User Experience contracts with proper messaging contracts used in your application.
        // Prefer using AutoMapper or the like for this 
        var args = new List<object>
        {
            new StartOnboardingRequest(value: req.Value)
        };
        
        
        var temporalClient = _httpContextAccessor.HttpContext?.Features.GetRequiredFeature<ITemporalClient>();
        
        var opts = new WorkflowOptions
        {
            // BestPractice: WorkflowIds should have business meaning.
            // Details: This identifier can be an AccountID, SessionID, etc.
            // 1. Prefer _pushing_ an WorkflowID down instead of retrieving after-the-fact.
            // 2. Acquaint your self with the "Workflow ID Reuse Policy" to fit your use case
            // Reference: https://docs.temporal.io/workflows#workflow-id-reuse-policy
            Id = req.OnboardingId,
            TaskQueue = "onboardings",
            // BestPractice: Do not fail a workflow on intermittent (eg bug) errors; prefer handling failures at the Activity level within the Workflow.
            // Details: A Workflow will very rarely need one to specify a RetryPolicy when starting a Workflow and we strongly discourage it.
            // Only Exceptions that inherit from `FailureException` will cause a RetryPolicy to be enforced. Other Exceptions will cause the WorkflowTask
            // to be rescheduled so that Workflows can continue to make progress once repaired/redeployed with corrections.
            // Reference: https://github.com/temporalio/sdk-dotnet/?tab=readme-ov-file#workflow-exceptions
            RetryPolicy = null,
        };
        var handle = await temporalClient.StartWorkflowAsync("WorkflowDefinitionDoesntExist", args, opts);
        // var output = await temporalClient.ExecuteWorkflowAsync(req);
        return Accepted(handle.ResultRunId);
    } 
    [HttpGet]
    public async Task<IActionResult> TestThingAsync()
    {
        var temporalClient = _httpContextAccessor.HttpContext.Features.Get<ITemporalClient>();
        
        Console.WriteLine("INSIDE TEST THING {0:G}", temporalClient?.Connection.IsConnected);
        var value = await  Task.FromResult("batman");
        return Ok(value);
       
    }
}
// var builder = WebApplication.CreateBuilder(args);
//
// // Setup console logging
// builder.Logging.AddSimpleConsole().SetMinimumLevel(LogLevel.Information);
//
// // Set a singleton for the client _task_. Errors will not happen here, only when
// // the await is performed.
// builder.Services.AddSingleton(async ctx => 
//     // TODO(cretz): It is not great practice to pass around tasks to be awaited
//     // on separately (VSTHRD003). We may prefer a direct DI extension, see
//     // https://github.com/temporalio/sdk-dotnet/issues/46.
//     await TemporalClient.ConnectAsync(new()
//     {
//         TargetHost = "localhost:7233",
//         LoggerFactory = ctx.GetRequiredService<ILoggerFactory>(),
//     }));
//
// var app = builder.Build();
//
// app.MapGet("/", async (ITemporalClient clientTask, string? name) =>
// {
//     var client = await clientTask;
//     return await client.ExecuteWorkflowAsync(
//         (MyWorkflow wf) => wf.RunAsync(name ?? "Temporal"),
//         new(id: $"aspnet-sample-workflow-{Guid.NewGuid()}", taskQueue: MyWorkflow.TaskQueue));
// });
//
// app.Run();