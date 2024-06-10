using Temporalio.Client;

namespace Temporal.Curriculum.Activities.Api.Middleware;

public class TemporalClientHttpMiddleware(RequestDelegate next, Task<TemporalClient> temporalClientTask)
{
    public async Task Invoke(HttpContext httpContext)
    {
        var temporalClient = await temporalClientTask;
        httpContext.Features.Set<ITemporalClient>(temporalClient);
        await next(httpContext);
    }
}