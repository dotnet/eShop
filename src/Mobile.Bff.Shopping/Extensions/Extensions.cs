internal static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHttpForwarderWithServiceDiscovery();

        builder.Services.AddHealthChecks()
            .AddUrlGroup(new Uri("http://catalog-api/health"), "catalogapi-check")
            .AddUrlGroup(new Uri("http://identity-api/health"), "identityapi-check");
    }
}
