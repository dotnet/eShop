using eShop.WebhookClient.Services;
using eShop.WebhookClient.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace eShop.Application.UnitTests;

[TestClass]
public class WebhookClientTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task HooksRepositoryStoresHooksAndNotifiesActiveSubscribers()
    {
        var repository = new HooksRepository();
        var notifications = 0;
        using var subscription = repository.Subscribe(() =>
        {
            notifications++;
            return Task.CompletedTask;
        });
        var hook = new WebHookReceived
        {
            Data = "payload",
            Token = "token",
            When = DateTime.UtcNow
        };

        await repository.AddNew(hook);

        Assert.AreEqual(1, notifications);
        CollectionAssert.Contains((await repository.GetAll()).ToList(), hook);
    }

    [TestMethod]
    public async Task DisposedSubscriptionIsNotNotified()
    {
        var repository = new HooksRepository();
        var notifications = 0;
        var subscription = repository.Subscribe(() =>
        {
            notifications++;
            return Task.CompletedTask;
        });
        subscription.Dispose();

        await repository.AddNew(new WebHookReceived());

        Assert.AreEqual(0, notifications);
    }

    [TestMethod]
    public async Task WebhookEndpointsValidateTokenAndPersistAcceptedPayload()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["ValidateToken"] = "true";
        builder.Configuration["WebhookClientOptions:Token"] = "expected";
        builder.Services.AddSingleton<HooksRepository>();
        await using var app = builder.Build();
        app.MapWebhookEndpoints();
        await app.StartAsync(TestContext.CancellationToken);
        var client = app.GetTestClient();

        using var invalidCheck = new HttpRequestMessage(HttpMethod.Options, "/check");
        invalidCheck.Headers.Add("X-eshop-whtoken", "wrong");
        var invalidCheckResponse = await client.SendAsync(invalidCheck, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.BadRequest, invalidCheckResponse.StatusCode);

        using var validCheck = new HttpRequestMessage(HttpMethod.Options, "/check");
        validCheck.Headers.Add("X-eshop-whtoken", "expected");
        var validCheckResponse = await client.SendAsync(validCheck, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, validCheckResponse.StatusCode);
        Assert.AreEqual("expected", validCheckResponse.Headers.GetValues("X-eshop-whtoken").Single());

        var payload = new WebhookData
        {
            When = DateTime.UtcNow,
            Payload = "payload",
            Type = "OrderPaid"
        };
        using var acceptedRequest = new HttpRequestMessage(HttpMethod.Post, "/webhook-received")
        {
            Content = JsonContent.Create(payload)
        };
        acceptedRequest.Headers.Add("X-eshop-whtoken", "expected");
        var acceptedResponse = await client.SendAsync(acceptedRequest, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, acceptedResponse.StatusCode);

        var repository = app.Services.GetRequiredService<HooksRepository>();
        var received = (await repository.GetAll()).Single();
        Assert.AreEqual("payload", received.Data);
        Assert.AreEqual("expected", received.Token);
    }
}
