using Temporalio.Client;

namespace Temporal.Curriculum.Workers.Api.Middleware;

public class TemporalClientHttpMiddleware(RequestDelegate next, ITemporalClient temporalClient)
{
    public async Task Invoke(HttpContext httpContext)
    {
        await temporalClient.Connection.ConnectAsync();
        httpContext.Features.Set(temporalClient);
        await next(httpContext);
    }
}