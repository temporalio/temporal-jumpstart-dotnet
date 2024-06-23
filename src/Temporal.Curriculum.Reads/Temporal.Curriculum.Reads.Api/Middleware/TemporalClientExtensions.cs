namespace Temporal.Curriculum.Reads.Api.Middleware;

public static class TemporalClientExtensions
{
    public static void UseTemporalClientHttpMiddleware(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<TemporalClientHttpMiddleware>();
    }
}