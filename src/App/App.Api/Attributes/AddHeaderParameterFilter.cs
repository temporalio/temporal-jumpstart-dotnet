using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace App.Api.Attributes;

// AddHeaderParameterFilter is used for Swagger exposure of the `AddHeaderParameter` attribute
public class AddHeaderParameterFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var addHeaders = context.MethodInfo.GetCustomAttributes(typeof(AddHeaderParameterAttribute), true).Cast<AddHeaderParameterAttribute>();
        foreach(var header in addHeaders)
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                In = ParameterLocation.Header,
                Schema = new OpenApiSchema { Type = "string" },
                Name = header.Name,
                Required = header.Required
            });
        }
    }
}