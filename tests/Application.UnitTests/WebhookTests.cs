using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Webhooks.API.Model;
using Webhooks.API.Services;

namespace eShop.Application.UnitTests;

[TestClass]
public class WebhookTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void SubscriptionRequestRejectsInvalidUrlsAndEvent()
    {
        var request = new WebhookSubscriptionRequest
        {
            Url = "not-a-url",
            GrantUrl = "also-not-a-url",
            Event = "not-an-event"
        };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(request, new ValidationContext(request), results, true);

        Assert.IsFalse(valid);
        Assert.HasCount(3, results);
    }

    [TestMethod]
    public async Task SenderPostsPayloadAndTokenToEveryReceiver()
    {
        var handler = new RecordingHandler();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));
        var sender = new WebhooksSender(factory, NullLogger<WebhooksSender>.Instance);
        var receivers = new[]
        {
            new WebhookSubscription
            {
                DestUrl = "https://receiver.test/hook",
                Token = "token",
                Type = WebhookType.OrderPaid
            }
        };
        var data = new WebhookData(WebhookType.OrderPaid, "payload");

        await sender.SendAll(receivers, data);

        Assert.HasCount(1, handler.Requests);
        var request = handler.Requests[0];
        Assert.AreEqual(HttpMethod.Post, request.Method);
        Assert.AreEqual("token", request.Headers.GetValues("X-eshop-whtoken").Single());
        Assert.Contains("payload", await request.Content!.ReadAsStringAsync(TestContext.CancellationToken));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
