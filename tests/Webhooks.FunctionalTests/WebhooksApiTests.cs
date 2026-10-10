using System.Net;
using System.Net.Http.Json;
using Asp.Versioning;
using Asp.Versioning.Http;

namespace eShop.Webhooks.FunctionalTests;

public sealed class WebhooksApiTests(WebhooksApiFixture fixture) : IClassFixture<WebhooksApiFixture>
{
    private HttpClient CreateClient(string userId)
    {
        var handler = new ApiVersionHandler(new QueryStringApiVersionWriter(), new ApiVersion(1.0));
        var client = fixture.CreateDefaultClient(handler);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);
        return client;
    }

    [Fact]
    public async Task SubscriptionLifecyclePersistsAndIsScopedToUser()
    {
        var owner = $"owner-{Guid.NewGuid():N}";
        using var ownerClient = CreateClient(owner);
        using var otherClient = CreateClient($"other-{Guid.NewGuid():N}");
        var request = new WebhookSubscriptionRequest
        {
            Url = "https://receiver.test/webhook",
            GrantUrl = "https://receiver.test/check",
            Token = "secret",
            Event = nameof(WebhookType.OrderPaid)
        };

        var createResponse = await ownerClient.PostAsJsonAsync(
            "/api/webhooks/",
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);
        var ownerGet = await ownerClient.GetAsync(createResponse.Headers.Location, TestContext.Current.CancellationToken);
        var otherGet = await otherClient.GetAsync(createResponse.Headers.Location, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, ownerGet.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherGet.StatusCode);

        var list = await ownerClient.GetFromJsonAsync<List<WebhookSubscription>>(
            "/api/webhooks/",
            TestContext.Current.CancellationToken);
        Assert.Contains(list!, subscription => subscription.UserId == owner);

        var deleteResponse = await ownerClient.DeleteAsync(
            createResponse.Headers.Location,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Accepted, deleteResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await ownerClient.GetAsync(createResponse.Headers.Location, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task InvalidGrantAndInvalidPayloadAreRejected()
    {
        using var client = CreateClient($"owner-{Guid.NewGuid():N}");
        var rejectedGrant = new WebhookSubscriptionRequest
        {
            Url = "https://receiver.test/webhook",
            GrantUrl = "https://receiver.test/reject",
            Token = "secret",
            Event = nameof(WebhookType.OrderPaid)
        };

        var grantResponse = await client.PostAsJsonAsync(
            "/api/webhooks/",
            rejectedGrant,
            TestContext.Current.CancellationToken);
        var validationResponse = await client.PostAsJsonAsync(
            "/api/webhooks/",
            new WebhookSubscriptionRequest
            {
                Url = "invalid",
                GrantUrl = "invalid",
                Event = "invalid"
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, grantResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, validationResponse.StatusCode);
    }
}
