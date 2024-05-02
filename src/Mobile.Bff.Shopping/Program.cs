var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddBasicServiceDefaults();  // Default health checks 

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.MapReverseProxy();

await app.RunAsync();
