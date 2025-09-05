using Onboardings.Domain.Workflows;
using Temporalio.Client;

namespace Onboardings.Api.Middleware;

public class TemporalClientHttpMiddleware(RequestDelegate next, ITemporalClient temporalClient)
{
    public async Task Invoke(HttpContext httpContext)
    {
        var userId = httpContext.Request.Headers["x-user-id"].ToString();
        if (!string.IsNullOrEmpty(userId))
        {
            IdentityContext.User.Value = new Identity
            {
                ClientId = Guid.NewGuid().ToString(),
                UserId = userId
            };
        }
        await temporalClient.Connection.ConnectAsync();
        httpContext.Features.Set(temporalClient);
        await next(httpContext);
    }
}