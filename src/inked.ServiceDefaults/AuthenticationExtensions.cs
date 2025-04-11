using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace inked.ServiceDefaults;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddDefaultAuthentication(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        var logger = services.BuildServiceProvider().GetRequiredService<ILogger<Type>>();

        var identitySection = configuration.GetSection("Identity");

        if (!identitySection.Exists())
        {
            // No identity section, so no authentication
            return services;
        }

        var scopes = identitySection.GetRequiredSection("Scopes").GetChildren().ToDictionary(p => p.Key, p => p.Value);
        JsonWebTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");
        if (identitySection.GetValue<bool>("JWT"))
        {
            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddKeycloakJwtBearer(
                    "keycloak",
                    "inked",
                    options =>
                    {
                        options.RequireHttpsMetadata = false;
                        options.Audience = identitySection.GetRequiredValue<string>("ClientId");
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            NameClaimType = "name", // Map the "name" claim to user.Identity.Name
                            RoleClaimType = "roles"
                        };
                    });
        }
        else
        {
            var oidcScheme = OpenIdConnectDefaults.AuthenticationScheme;

            services.AddAuthentication(oidcScheme)
                .AddKeycloakOpenIdConnect("keycloak", "inked", oidcScheme, options =>
                {
                    options.ClientId = identitySection.GetRequiredValue<string>("ClientId");
                    options.ClientSecret = identitySection.GetRequiredValue<string>("ClientSecret");
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    foreach (var scope in scopes.Keys)
                    {
                        options.Scope.Add(scope);
                    }

                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                    options.SaveTokens = true;
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);
        }


        services.AddAuthorization(options =>
        {
            foreach (var scope in scopes.Keys)
            {
                var policy = $"require-{scope}";
                options.AddPolicy(policy, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", scope);
                });
                logger.LogInformation($"Authorization policy: {policy} created!");
            }
        });
        services.AddCascadingAuthenticationState();
        services.AddAuthorizationBuilder();
        return services;
    }
}

public class AuthorizationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null)
        {
            var accessToken = await context.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
