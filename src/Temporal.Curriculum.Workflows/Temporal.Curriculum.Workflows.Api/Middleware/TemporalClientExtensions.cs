namespace Temporal.Curriculum.Workflows.Api.Middleware;

public static class TemporalClientExtensions
{
    public static IApplicationBuilder UseTemporalClientHttpMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TemporalClientHttpMiddleware>();
    }
}