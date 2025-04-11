using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

public class LoggingAuthorizationHandler : AuthorizationHandler<IAuthorizationRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<LoggingAuthorizationHandler> _logger;

    public LoggingAuthorizationHandler(IHttpContextAccessor httpContextAccessor,
        ILogger<LoggingAuthorizationHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        IAuthorizationRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HTTP context is null.");
            return Task.CompletedTask;
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation("User is authenticated.");
        }
        else
        {
            _logger.LogWarning("User is NOT authenticated.");
        }

        if (context.User.Identity is ClaimsIdentity identity)
        {
            foreach (var claim in identity.Claims)
            {
                _logger.LogInformation($"Claim: {claim.Type} = {claim.Value}");
            }
        }

        if (requirement is ClaimsAuthorizationRequirement claimsRequirement)
        {
            foreach (var claim in claimsRequirement.AllowedValues)
            {
                if (context.User.HasClaim(claimsRequirement.ClaimType, claim))
                {
                    _logger.LogInformation($"User has claim {claimsRequirement.ClaimType}: {claim}");
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }

            _logger.LogWarning(
                $"User does not have required claim {claimsRequirement.ClaimType}: {string.Join(", ", claimsRequirement.AllowedValues)}");
            return Task.CompletedTask;
        }

        _logger.LogInformation("Requirement is not a claims requirement, Succeeding.");
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
