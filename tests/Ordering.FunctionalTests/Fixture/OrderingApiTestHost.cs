using eShop.Ordering.API;

using Microsoft.AspNetCore.Mvc.Testing;

namespace eShop.Ordering.FunctionalTests;

public sealed class OrderingApiTestHost(OrderingApiFixture fixture, HttpClient client)
{
    public OrderingApiFixture Fixture { get; } = fixture;

    public HttpClient Client { get; } = client;

    public WebApplicationFactory<Program> WebApplicationFactory => Fixture;
}
