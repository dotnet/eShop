var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddApplicationServices();
builder.Services.AddProblemDetails();

// Add CORS for Shipper UI and Admin UI
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var withApiVersioning = builder.Services.AddApiVersioning();

builder.AddDefaultOpenApi(withApiVersioning);

var app = builder.Build();

app.UseCors();

app.MapDefaultEndpoints();

var shipping = app.NewVersionedApi("Shipping");

shipping.MapShipmentApiV1()
    .RequireAuthorization();

shipping.MapShipperApiV1()
    .RequireAuthorization();

app.UseDefaultOpenApi();
app.Run();
