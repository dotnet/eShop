var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddApplicationServices();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapDefaultEndpoints();
app.MapReverseProxy();

await app.RunAsync();
