using Temporalio.Client;

namespace Temporal.Curriculum.Starters.Clients;

public static class TemporalClientExtensions
{
    public static IApplicationBuilder UseTemporalClientHTTPMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TemporalClientHttpMiddleware>();
    }
}