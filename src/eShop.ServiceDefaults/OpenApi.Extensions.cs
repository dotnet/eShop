using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace eShop.ServiceDefaults;

public static partial class Extensions
{
    public static IApplicationBuilder UseDefaultOpenApi(this WebApplication app)
    {
        var configuration = app.Configuration;
        var openApiSection = configuration.GetSection("OpenApi");

        if (!openApiSection.Exists())
        {
            return app;
        }

        app.MapOpenApi().WithDocumentPerVersion();

        if (app.Environment.IsDevelopment())
        {
            var descriptions = app.DescribeApiVersions();
            var defaultDocument = descriptions.Count > 0 ? descriptions[^1].GroupName : "v1";

            app.MapScalarApiReference(options =>
            {
                // Disable default fonts to avoid download unnecessary fonts
                options.DefaultFonts = false;

                foreach (var description in descriptions)
                {
                    options.AddDocument(description.GroupName, description.GroupName, isDefault: description.GroupName == defaultDocument);
                }
            });
            app.MapGet("/", () => Results.Redirect($"/scalar/{defaultDocument}")).ExcludeFromDescription();
        }

        return app;
    }

    public static IHostApplicationBuilder AddDefaultOpenApi(
        this IHostApplicationBuilder builder,
        IApiVersioningBuilder? apiVersioning = default)
    {
        var openApi = builder.Configuration.GetSection("OpenApi");
        var identitySection = builder.Configuration.GetSection("Identity");

        var scopes = identitySection.Exists()
            ? identitySection.GetRequiredSection("Scopes").GetChildren().ToDictionary(p => p.Key, p => p.Value)
            : new Dictionary<string, string?>();


        if (!openApi.Exists())
        {
            return builder;
        }

        if (apiVersioning is not null)
        {
            // the default format will just be ApiVersion.ToString(); for example, 1.0.
            // this will format the version as "'v'major[.minor][-status]"
            apiVersioning.AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.DefaultApiVersionParameterDescription = "The API version, in the format 'major.minor'.";
                })
                .AddOpenApi(options =>
                {
                    var document = options.Document;

                    document.ApplyApiVersionInfo(openApi.GetRequiredValue("Document:Title"), openApi.GetRequiredValue("Document:Description"));
                    document.ApplyAuthorizationChecks([.. scopes.Keys]);
                    document.ApplySecuritySchemeDefinitions();
                    document.ApplyOperationDeprecatedStatus();
                    document.ApplyApiVersionDescription();
                });
        }

        return builder;
    }
}
