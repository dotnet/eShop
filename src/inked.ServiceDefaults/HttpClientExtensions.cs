using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace inked.ServiceDefaults;

public static class HttpClientExtensions
{
    public static IHttpClientBuilder AddAuthToken(this IHttpClientBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();

        builder.Services.TryAddTransient<HttpClientAuthorizationDelegatingHandler>();

        builder.AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>();

        return builder;
    }

    private class HttpClientAuthorizationDelegatingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<HttpClientAuthorizationDelegatingHandler> _logger;

        public HttpClientAuthorizationDelegatingHandler(IHttpContextAccessor httpContextAccessor,
            ILogger<HttpClientAuthorizationDelegatingHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // You can also add a second constructor if you need to support inner handlers, as you've done.
        public HttpClientAuthorizationDelegatingHandler(IHttpContextAccessor httpContextAccessor,
            ILogger<HttpClientAuthorizationDelegatingHandler> logger, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }


        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_httpContextAccessor.HttpContext is HttpContext context)
            {
                var accessToken = await context.GetTokenAsync("access_token");

                if (accessToken != null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    _logger.LogInformation("Authorization token added to request.");
                }
                else
                {
                    _logger.LogWarning("No authorization token found in HttpContext.");
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
