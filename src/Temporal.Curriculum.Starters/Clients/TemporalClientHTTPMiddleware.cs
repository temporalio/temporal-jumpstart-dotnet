using Temporalio.Client;

namespace Temporal.Curriculum.Starters.Clients;

public class TemporalClientHttpMiddleware(RequestDelegate next, Task<TemporalClient> temporalClientTask)
{
    public async Task Invoke(HttpContext httpContext)
    {
        var temporalClient = await temporalClientTask;
        httpContext.Features.Set<ITemporalClient>(temporalClient);
        Console.WriteLine("INSIDE MIDDLEWARE");

        await next(httpContext);
        Console.WriteLine("AFTER MIDDLEWARE");
    }
}