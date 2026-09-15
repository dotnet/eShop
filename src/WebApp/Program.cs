using eShop.WebApp.Components;
using eShop.ServiceDefaults;
using Microsoft.FeatureManagement;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.AddApplicationServices();
builder.AddAzureAppConfiguration(
    "appconfig",
    configureOptions: options =>
    {
        options.UseFeatureFlags();
        // options.UseFeatureFlags(config => config.Select("*", builder.Environment.EnvironmentName));

        // Configure refresh
        options.ConfigureRefresh(refresh =>
        {
            refresh.Register("eShop:Sentinel", refreshAll: true)
                .SetRefreshInterval(TimeSpan.FromSeconds(30));
        });
    });

builder.Services.AddFeatureManagement()
    .AddFeatureFilter<RingDeploymentFeatureFilter>();

builder.Services.Configure<ConfigurationFeatureDefinitionProviderOptions>(o =>
{
    o.CustomConfigurationMergingEnabled = true;
});

builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
    tracing.AddSource("Microsoft.FeatureManagement"));

Console.WriteLine(((IConfigurationRoot)builder.Configuration).GetDebugView());

var app = builder.Build();

app.UseAzureAppConfiguration();

var manager = app.Services.GetRequiredService<IFeatureManager>();
await foreach (var name in manager.GetFeatureNamesAsync())
{
    var enabled = await manager.IsEnabledAsync(name);
    Console.WriteLine($"Feature {name} is {(enabled ? "enabled" : "disabled")}");
}

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseAntiforgery();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapForwarder("/product-images/{id}", "https+http://catalog-api", "/api/catalog/items/{id}/pic");

app.Run();
