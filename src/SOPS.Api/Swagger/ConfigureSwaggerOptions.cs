using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SOPS.Api.Swagger;

/// <summary>
/// Creates one OpenAPI document per SOPS API version (v1, v2, …) for Swagger UI.
/// </summary>
public sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "SOPS — Smart Order Processing System",
                Version = description.ApiVersion.ToString(),
                Description = description.IsDeprecated
                    ? "This API version is deprecated."
                    : "Enterprise order management API (.NET 8)"
            });
        }

        // Each action appears only in its API version group (v1 vs v2).
        options.DocInclusionPredicate((docName, apiDesc) =>
            string.Equals(docName, apiDesc.GroupName, StringComparison.OrdinalIgnoreCase));
    }
}
