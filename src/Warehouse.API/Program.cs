var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddApplicationServices();
builder.Services.AddProblemDetails();

// Add CORS for Admin UI
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
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

var warehouse = app.NewVersionedApi("Warehouse");

warehouse.MapWarehouseApiV1()
    .RequireAuthorization();

app.UseDefaultOpenApi();
app.Run();
