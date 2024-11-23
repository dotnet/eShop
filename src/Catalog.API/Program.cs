using Asp.Versioning.Builder;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

if (Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider")
{
    // Add a dummy DB context
    builder.Services.AddDbContext<CatalogContext>();
}
else
{
    builder.AddServiceDefaults();
    builder.AddApplicationServices();
}
builder.Services.AddProblemDetails();

var withApiVersioning = builder.Services.AddApiVersioning();

builder.AddDefaultOpenApi(withApiVersioning);

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseStatusCodePages();

app.NewVersionedApi("Catalog")
   .MapCatalogApiV1();

app.UseDefaultOpenApi();
app.Run();
