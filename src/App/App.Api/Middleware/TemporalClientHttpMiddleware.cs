using App.Domain.Workflows;
using Temporalio.Client;

namespace App.Api.Middleware;

public class TemporalClientHttpMiddleware(RequestDelegate next, ITemporalClient temporalClient)
{
    public async Task Invoke(HttpContext httpContext)
    {
        var userId = httpContext.Request.Headers["x-user-id"].ToString();
        if (!string.IsNullOrEmpty(userId))
        {
            // See ContextPropagationInterceptor<Identity> to see how this
            // value gets passed down through Workflows and Activities
            IdentityContext.User.Value = new Identity
            {
                // This could just as easily be an environment variable
                ClientId = Guid.NewGuid().ToString(),
                UserId = userId
            };
        }
        await temporalClient.Connection.ConnectAsync();
        httpContext.Features.Set(temporalClient);
        await next(httpContext);
    }
}