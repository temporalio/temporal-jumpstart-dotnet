using System.Diagnostics;
using Temporal.Curriculum.Workers.Domain.Clients.Temporal;
using Temporalio.Client;

namespace Temporal.Curriculum.Workers.Api.Middleware;

public static class TemporalClientExtensions
{
    public static IApplicationBuilder UseTemporalClientHttpMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TemporalClientHttpMiddleware>();
    }
}