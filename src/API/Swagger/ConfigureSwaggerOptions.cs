using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Swagger;

/// <summary>
/// Generates one Swagger document per discovered API version. The versioning
/// ApiExplorer hands us the set of versions (currently just v1); for each one we
/// register a SwaggerDoc so the UI shows the correct definition. Implemented as
/// IConfigureOptions so it can depend on the version provider via DI rather than
/// hard-coding "v1" in Program.cs.
/// </summary>
public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) => _provider = provider;

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "CRN Products API",
                Version = description.ApiVersion.ToString(),
                Description = "Products API for the CRN Technosoft .NET assessment."
            });
        }
    }
}
