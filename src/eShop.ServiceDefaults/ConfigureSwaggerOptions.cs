using System.Text;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace eShop.ServiceDefaults;

internal sealed class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;
    private readonly IConfiguration _configuration;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider, IConfiguration configuration)
    {
        _provider = provider;
        _configuration = configuration;
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
        }

        ConfigureAuthorization(options);
    }

    private OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        /// {
        ///   "OpenApi": {
        ///     "Document": {
        ///         "Title": ..
        ///         "Version": ..
        ///         "Description": ..
        ///     }
        ///   }
        /// }
        var openApi = _configuration.GetSection("OpenApi");
        var document = openApi.GetRequiredSection("Document");
        var info = new OpenApiInfo()
        {
            Title = document.GetRequiredValue("Title"),
            Version = description.ApiVersion.ToString(),
            Description = BuildDescription(description, document.GetRequiredValue("Description")),
        };

        return info;
    }

    private static string BuildDescription(ApiVersionDescription api, string description)
    {
        var text = new StringBuilder(description);

        if (api.IsDeprecated)
        {
            if (text.Length > 0)
            {
                if (text[^1] != '.')
                {
                    text.Append('.');
                }

                text.Append(' ');
            }

            text.Append("This API version has been deprecated.");
        }

        if (api.SunsetPolicy is { } policy)
        {
            if (policy.Date is { } when)
            {
                if (text.Length > 0)
                {
                    text.Append(' ');
                }

                text.Append("The API will be sunset on ")
                    .Append(when.Date.ToShortDateString())
                    .Append('.');
            }

            if (policy.HasLinks)
            {
                text.AppendLine();

                var rendered = false;

                foreach (var link in policy.Links.Where(l => l.Type == "text/html"))
                {
                    if (!rendered)
                    {
                        text.Append("<h4>Links</h4><ul>");
                        rendered = true;
                    }

                    text.Append("<li><a href=\"");
                    text.Append(link.LinkTarget.OriginalString);
                    text.Append("\">");
                    text.Append(
                        StringSegment.IsNullOrEmpty(link.Title)
                        ? link.LinkTarget.OriginalString
                        : link.Title.ToString());
                    text.Append("</a></li>");
                }

                if (rendered)
                {
                    text.Append("</ul>");
                }
            }
        }

        return text.ToString();
    }

    private void ConfigureAuthorization(SwaggerGenOptions options)
    {
        var identitySection = _configuration.GetSection("Identity");

        if (!identitySection.Exists())
        {
            // No identity section, so no authentication open api definition
            return;
        }

        // {
        //   "Identity": {
        //     "Url": "http://identity",
        //     "Scopes": {
        //         "basket": "Basket API"
        //      }
        //    }
        // }

        var identityUrlExternal = identitySection.GetRequiredValue("Url");
        var scopes = identitySection.GetRequiredSection("Scopes").GetChildren().ToDictionary(p => p.Key, p => p.Value);

        options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows()
            {
                // TODO: Change this to use Authorization Code flow with PKCE
                Implicit = new OpenApiOAuthFlow()
                {
                    AuthorizationUrl = new Uri($"{identityUrlExternal}/connect/authorize"),
                    TokenUrl = new Uri($"{identityUrlExternal}/connect/token"),
                    Scopes = scopes,
                }
            }
        });

        options.OperationFilter<AuthorizeCheckOperationFilter>([scopes.Keys.ToArray()]);
    }

    private sealed class AuthorizeCheckOperationFilter(string[] scopes) : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

            if (!metadata.OfType<IAuthorizeData>().Any())
            {
                return;
            }

            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });

            var oAuthScheme = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
            };

            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new()
                {
                    [ oAuthScheme ] = scopes
                }
            };
        }
    }
}
