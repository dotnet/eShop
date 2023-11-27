var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddApplicationServices();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapDefaultEndpoints();

const string CatalogApiPrefix = "/api/v1/catalog/";
var forwardedCatalogApis = new[]
{
    CatalogApiPrefix + "items",
    CatalogApiPrefix + "items/by",
    CatalogApiPrefix + "items/{id}",
    CatalogApiPrefix + "items/by/{name}",

    CatalogApiPrefix + "items/withsemanticrelevance/{text}",

    CatalogApiPrefix + "items/type/{typeId}/brand/{brandId?}",
    CatalogApiPrefix + "items/type/all/brand/{brandId?}",
    CatalogApiPrefix + "catalogTypes",
    CatalogApiPrefix + "catalogBrands",

    CatalogApiPrefix + "items/{id}/pic",
};

foreach (var forwardedUrl in forwardedCatalogApis)
{
    var mapFromPattern = "/catalog-api" + forwardedUrl;
    var mapToTargetPath = forwardedUrl;

    app.MapForwarder(mapFromPattern, "http://catalog-api", mapToTargetPath);
}

await app.RunAsync();
