using Asp.Versioning.Builder;
using eShop.Catalog.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddApplicationServices();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<ISaleService, SaleService>();

var withApiVersioning = builder.Services.AddApiVersioning();

builder.AddDefaultOpenApi(withApiVersioning);

var app = builder.Build();

app.MapDefaultEndpoints();

app.NewVersionedApi("Catalog")
   .MapCatalogApiV1();

app.UseDefaultOpenApi();
app.Run();
