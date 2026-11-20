using eShop.Identity.API.Configuration;
using eShop.Identity.API.Services;
using Microsoft.Extensions.Configuration;

namespace eShop.Application.UnitTests;

[TestClass]
public class IdentityConfigurationTests
{
    [TestMethod]
    public void ApiResourcesAndScopesStayAligned()
    {
        var resources = Config.GetApis().Select(resource => resource.Name).Order().ToArray();
        var scopes = Config.GetApiScopes().Select(scope => scope.Name).Order().ToArray();

        CollectionAssert.AreEqual(resources, scopes);
        CollectionAssert.AreEquivalent(new[] { "basket", "orders", "webhooks" }, scopes);
    }

    [TestMethod]
    public void ClientsUseConfiguredCallbackUrlsAndExpectedScopes()
    {
        var values = new Dictionary<string, string?>
        {
            ["MauiCallback"] = "maui://callback",
            ["WebAppClient"] = "https://webapp.test",
            ["WebhooksWebClient"] = "https://webhooks-client.test",
            ["BasketApiClient"] = "https://basket.test",
            ["OrderingApiClient"] = "https://ordering.test",
            ["WebhooksApiClient"] = "https://webhooks.test"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var clients = Config.GetClients(configuration).ToDictionary(client => client.ClientId);

        CollectionAssert.Contains(clients["webapp"].RedirectUris.ToList(), "https://webapp.test/signin-oidc");
        CollectionAssert.Contains(clients["webhooksclient"].AllowedScopes.ToList(), "webhooks");
        CollectionAssert.Contains(clients["maui"].AllowedScopes.ToList(), "basket");
        CollectionAssert.Contains(clients["maui"].AllowedScopes.ToList(), "orders");
    }

    [TestMethod]
    [DataRow("/connect/authorize?redirect_uri=https%3A%2F%2Fweb.test%2Fsignin-oidc&scope=openid", "https://web.test/")]
    [DataRow("/connect/authorize?client_id=webapp", "")]
    public void RedirectServiceExtractsConfiguredRedirect(string returnUrl, string expected)
    {
        var service = new RedirectService();

        Assert.AreEqual(expected, service.ExtractRedirectUriFromReturnUrl(returnUrl));
    }
}
