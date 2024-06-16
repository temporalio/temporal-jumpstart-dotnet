using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Temporal.Curriculum.Activities.Api.Middleware;
using Temporal.Curriculum.Activities.Domain.Clients.Temporal;
using Temporalio.Client;

namespace Temporal.Curriculum.Activities.Api;

public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
public void ConfigureServices(IServiceCollection services)
        {
            const string temporalConfigSection = "Temporal";
            var temporalConfig = Configuration.GetRequiredSection(temporalConfigSection).Get<TemporalConfig>();
            Debug.Assert(temporalConfig!=null);
            services.AddOptions<TemporalConfig>().BindConfiguration(temporalConfigSection);
            services.AddHttpContextAccessor();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddTemporalClient(o =>
            {
                o.ConfigureClient(temporalConfig);
            }).Configure<ITemporalClient>(c =>
            {
                // connect when container is built
                c.Connection.ConnectAsync();
            });
           
            
            services.AddControllers(setupAction =>
                {
                    setupAction.ReturnHttpNotAcceptable = true;

                }).AddJsonOptions(setupAction =>
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
                            context as Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext;

                       
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


        internal static IActionResult ProblemDetailsInvalidModelStateResponse(
            ProblemDetailsFactory problemDetailsFactory, ActionContext context)
        {
            var problemDetails = problemDetailsFactory.CreateValidationProblemDetails(context.HttpContext, context.ModelState);
            ObjectResult result;
            if (problemDetails.Status == 400)
            {
                // For compatibility with 2.x, continue producing BadRequestObjectResult instances if the status code is 400.
                result = new BadRequestObjectResult(problemDetails);
            }
            else
            {
                result = new ObjectResult(problemDetails);
            }
            result.ContentTypes.Add("application/problem+json");
            result.ContentTypes.Add("application/problem+xml");

            return result;
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
            app.UseEndpoints(endpoints =>
            {   
                endpoints.MapControllers();
            });
        }
    }

    