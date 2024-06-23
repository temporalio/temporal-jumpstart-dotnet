using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Temporal.Curriculum.Writes.Api.Middleware;
using Temporal.Curriculum.Writes.Domain.Clients.Temporal;
using Temporalio.Client;

namespace Temporal.Curriculum.Writes.Api;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    private IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        const string temporalConfigSection = "Temporal";
        var temporalConfig = Configuration.GetRequiredSection(temporalConfigSection).Get<TemporalConfig>();
        Debug.Assert(temporalConfig != null);
        services.AddOptions<TemporalConfig>().BindConfiguration(temporalConfigSection);
        services.AddHttpContextAccessor();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddTemporalClient(o => { o.ConfigureClient(temporalConfig); }).Configure<ITemporalClient>(c =>
        {
            // connect when container is built
            c.Connection.ConnectAsync();
        });


        services.AddControllers(setupAction => { setupAction.ReturnHttpNotAcceptable = true; }).AddJsonOptions(
                setupAction =>
                {
                    setupAction.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    setupAction.JsonSerializerOptions.WriteIndented = true;
                })
            .ConfigureApiBehaviorOptions(setupAction =>
            {
                // this was shamelessly lifted from here
                // https://github.com/KevinDockx/BuildingRESTfulAPIAspNetCore3/blob/master/Finished%20sample/CourseLibrary/CourseLibrary.API/Startup.cs

                setupAction.InvalidModelStateResponseFactory = context =>
                {
                    // create a problem details object
                    var problemDetailsFactory = context.HttpContext.RequestServices
                        .GetRequiredService<ProblemDetailsFactory>();
                    var problemDetails = problemDetailsFactory.CreateValidationProblemDetails(
                        context.HttpContext,
                        context.ModelState);

                    // add additional info not added by default
                    problemDetails.Detail = "See the errors field for details.";
                    problemDetails.Instance = context.HttpContext.Request.Path;

                    // find out which status code to use
                    var actionExecutingContext =
                        context as ActionExecutingContext;


                    // only validation errors should be here
                    if (context.ModelState.ErrorCount > 0 &&
                        (context is ControllerContext ||
                         actionExecutingContext?.ActionArguments.Count ==
                         context.ActionDescriptor.Parameters.Count))
                    {
                        problemDetails.Type = "https://myapi.com/path/to/modelrequirements";
                        problemDetails.Status = StatusCodes.Status422UnprocessableEntity;
                        problemDetails.Title = "One or more validation errors occurred.";

                        return new UnprocessableEntityObjectResult(problemDetails)
                        {
                            ContentTypes = { "application/problem+json" }
                        };
                    }

                    // if one of the keys wasn't correctly found / couldn't be parsed
                    // we're dealing with null/unparsable input
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "One or more errors on input occurred.";
                    return new BadRequestObjectResult(problemDetails)
                    {
                        ContentTypes = { "application/problem+json" }
                    };
                };
            });
    }


    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // expose the Temporal Client in the request Features set 
        app.UseTemporalClientHttpMiddleware();


        app.UseSwagger();
        app.UseSwaggerUI();
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler(appBuilder =>
            {
                appBuilder.Run(async context =>
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("An unexpected fault happened. Try again later.");
                });
            });
        }

        app.UseRouting();

        // app.UseAuthorization()
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}