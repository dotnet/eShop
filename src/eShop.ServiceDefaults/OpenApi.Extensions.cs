using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        app.MapOpenApi();

        if (app.Environment.IsDevelopment())
        {
            var authSection = openApiSection.GetSection("Auth");
            app.MapSwaggerUi(authSection);
        }

        return app;
    }

    private static IEndpointConventionBuilder MapSwaggerUi(this IEndpointRouteBuilder endpoints, IConfigurationSection authConfigurationSection)
    {
        return endpoints.MapGet("/swagger/{documentName}", (string documentName) => Results.Content($$"""
    <html>
    <head>
        <meta charset="UTF-8">
        <title>OpenAPI -- {{documentName}}</title>
        <link rel="stylesheet" type="text/css" href="https://unpkg.com/swagger-ui-dist/swagger-ui.css">
    </head>
    <body>
        <div id="swagger-ui"></div>

        <script src="https://unpkg.com/swagger-ui-dist/swagger-ui-standalone-preset.js"></script>
        <script src="https://unpkg.com/swagger-ui-dist/swagger-ui-bundle.js"></script>

        <script>
            window.onload = function() {
                const ui = SwaggerUIBundle({
                    url: "/openapi/{{documentName}}.json",
                        dom_id: '#swagger-ui',
                        deepLinking: true,
                        presets: [
                            SwaggerUIBundle.presets.apis,
                            SwaggerUIStandalonePreset
                        ],
                        plugins: [
                            SwaggerUIBundle.plugins.DownloadUrl
                        ],
                        layout: "StandaloneLayout",
                });
                {{InitializeOAuth(authConfigurationSection)}}
                window.ui = ui
            }
        </script>
    </body>
    </html>
    """, "text/html")).ExcludeFromDescription();

        static string InitializeOAuth(IConfigurationSection configurationSection)
        {
            if (configurationSection.Exists())
            {
                var clientId = configurationSection.GetRequiredValue("ClientId");
                var appName = configurationSection.GetRequiredValue("AppName");
                return $@"
                    ui.initOAuth({{
                            appName: ""{appName}"",
                            clientId: ""{clientId}"",
                            appName: ""{appName}"",
                        }});
                    ";
            }
            return string.Empty;
        }
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
            var versioned = apiVersioning.AddApiExplorer(options => options.GroupNameFormat = "'v'VVV");
            string[] versions = ["v1"];
            foreach (var description in versions)
            {
                builder.Services.AddOpenApi(description, options =>
                {
                    options.ApplyApiVersionInfo(openApi.GetRequiredValue("Document:Title"), openApi.GetRequiredValue("Document:Description"));
                    options.ApplyAuthorizationChecks([.. scopes.Keys]);
                    options.ApplySecuritySchemeDefinitions();
                    options.ApplyOperationDefaultValues();
                });
            }
        }

        return builder;
    }

    internal static T GetService<T>(this IServiceCollection services)
    {
        var descriptor = services.LastOrDefault(d => typeof(T).IsAssignableFrom(d.ServiceType) || d.ServiceType == typeof(T));
        if (descriptor is { ImplementationInstance: T instance })
        {
            return instance;
        }
        else
        {
            return (T)descriptor!.ImplementationFactory!(services.BuildServiceProvider());
        }
    }
}
