using System.Diagnostics;
using System.Security.Principal;
using App.Domain.Workflows;

namespace App.Domain.Clients.Temporal;

using System.Threading.Tasks;
using Temporalio.Api.Common.V1;
using Temporalio.Client;
using Temporalio.Client.Interceptors;
using Temporalio.Converters;
using Temporalio.Worker.Interceptors;
using Temporalio.Workflows;

/// <summary>
/// Application support interceptor that can be used to propagate async-local context through workflows
/// and activities. This must be set on the client used for interacting with workflows and used for
/// the worker.
/// </summary>
public class ApplicationContextInterceptor : IWorkerInterceptor, IClientInterceptor
{
    public WorkflowInboundInterceptor InterceptWorkflow(WorkflowInboundInterceptor nextInterceptor)
    {
        return new ApplicationContextWorkflowInboundInterceptor(this, nextInterceptor);
    }

    public ActivityInboundInterceptor InterceptActivity(ActivityInboundInterceptor nextInterceptor)
    {
        return new ApplicationContextActivityInboundInterceptor(this, nextInterceptor);
    }

    private class ApplicationContextWorkflowInboundInterceptor : WorkflowInboundInterceptor
    {
        private readonly ApplicationContextInterceptor root;

        public ApplicationContextWorkflowInboundInterceptor(ApplicationContextInterceptor root, WorkflowInboundInterceptor next)
            : base(next)
        {
            this.root = root;
        }
    }

    private class ApplicationContextActivityInboundInterceptor : ActivityInboundInterceptor
    {
        private readonly ApplicationContextInterceptor root;

        public ApplicationContextActivityInboundInterceptor(
            ApplicationContextInterceptor root, ActivityInboundInterceptor next)
            : base(next) => this.root = root;

        public override Task<object?> ExecuteActivityAsync(ExecuteActivityInput input)
        {
            // Yank out the identity that has been passed down (serialized) from the caller
            var name = $"{IdentityContext.User.Value?.UserId} : {IdentityContext.User.Value?.ClientId}";
            // Here is where we could fetch the Principal from a database or other source
            // the same as you'd do to set the `Application.CurrentPrincipal` in a web app.
            var id = new GenericIdentity(name);
            var roles = new[] { "admin", "user" };
            ApplicationContext.CurrentPrincipal.Value = new GenericPrincipal(id, roles);
            // ApplicationContext.CurrentDbContext.Value = new DbContext(new DbContextOptions<>())
            return Next.ExecuteActivityAsync(input);
        }
    }
}