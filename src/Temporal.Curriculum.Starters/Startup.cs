using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using Temporal.Curriculum.Starters.Clients;
using Temporal.Curriculum.Starters.Config;
using Temporalio.Client;
using Temporalio.Extensions.Hosting;
namespace Temporal.Curriculum.Starters;

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
            services.Configure<TemporalConfig>(Configuration.GetSection("Temporal"));
            services.AddHttpContextAccessor();
            services.AddSingleton( ctx =>
            {
                var config = ctx.GetRequiredService<IOptions<TemporalConfig>>();
                var loggerFactory = ctx.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("temporal_cfg");
                logger.LogInformation("connecting to temporal namespace {config.Connection.Namespace}", config.Value.Connection.Namespace);
                var opts = new TemporalClientConnectOptions
                {
                    Namespace = config.Value.Connection.Namespace,
                    TargetHost = config.Value.Connection.Target,
                    LoggerFactory = ctx.GetRequiredService<ILoggerFactory>(),
                };
                if (config.Value.Connection.Mtls != null)
                {
                    logger.LogInformation("using cert from {config.Connection.Mtls.CertChainFile}", config.Value.Connection.Mtls.CertChainFile);

                    opts.Tls = new TlsOptions
                    {
                        ClientCert =  File.ReadAllBytes(config.Value.Connection.Mtls.CertChainFile),
                        ClientPrivateKey =  File.ReadAllBytes(config.Value.Connection.Mtls.KeyFile),
                    };
                }
                
                return TemporalClient.ConnectAsync(opts);
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

            // services.AddScoped<ICourseLibraryRepository, CourseLibraryRepository>();

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
            
            app.UseTemporalClientHTTPMiddleware();
            
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

    