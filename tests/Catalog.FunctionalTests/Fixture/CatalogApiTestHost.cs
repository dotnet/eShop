using eShop.Catalog.API;

using Microsoft.AspNetCore.Mvc.Testing;

namespace eShop.Catalog.FunctionalTests;

public sealed class CatalogApiTestHost(CatalogApiFixture fixture, HttpClient client)
{
    public CatalogApiFixture Fixture { get; } = fixture;

    public HttpClient Client { get; } = client;

    public WebApplicationFactory<Program> WebApplicationFactory => Fixture;
}
