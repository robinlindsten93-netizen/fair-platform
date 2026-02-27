using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fair.Api.Swagger;

public sealed class OtpExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody?.Content is null) return;
        if (!operation.RequestBody.Content.TryGetValue("application/json", out var json)) return;

        var route = context.ApiDescription.RelativePath ?? "";

        if (route.Contains("api/v1/auth/otp/request", StringComparison.OrdinalIgnoreCase))
        {
            json.Example = new OpenApiObject
            {
                ["phone"] = new OpenApiString("+46701234567")
            };
        }

        if (route.Contains("api/v1/auth/otp/verify", StringComparison.OrdinalIgnoreCase))
        {
            json.Example = new OpenApiObject
            {
                ["phone"] = new OpenApiString("+46701234567"),
                ["code"]  = new OpenApiString("123456")
            };
        }
    }
}