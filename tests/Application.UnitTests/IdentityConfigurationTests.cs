using System.Security.Claims;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using eShop.Identity.API.Configuration;
using eShop.Identity.API.Services;
using IdentityServerHost.Quickstart.UI;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        Assert.AreSequenceEqual(resources, scopes);
        Assert.AreSequenceEqual(new[] { "basket", "orders", "webhooks" }, scopes);
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

        Assert.Contains("https://webapp.test/signin-oidc", clients["webapp"].RedirectUris);
        Assert.Contains("webhooks", clients["webhooksclient"].AllowedScopes);
        Assert.Contains("basket", clients["maui"].AllowedScopes);
        Assert.Contains("orders", clients["maui"].AllowedScopes);
    }

    [TestMethod]
    [DataRow("/connect/authorize?redirect_uri=https%3A%2F%2Fweb.test%2Fsignin-oidc&scope=openid", "https://web.test/")]
    [DataRow("/connect/authorize?client_id=webapp", "")]
    public void RedirectServiceExtractsConfiguredRedirect(string returnUrl, string expected)
    {
        var service = new RedirectService();

        Assert.AreEqual(expected, service.ExtractRedirectUriFromReturnUrl(returnUrl));
    }

    [TestMethod]
    [DataRow("https://web.test/signin-oidc", false)]
    [DataRow("maui://callback", true)]
    public async Task CancellingLoginDeniesAuthenticationWithRequestCancellation(string redirectUri, bool nativeClient)
    {
        const string returnUrl = "/connect/authorize/callback?client_id=webapp";
        using var cancellation = new CancellationTokenSource();
        var interaction = Substitute.For<IIdentityServerInteractionService>();
        var request = new AuthorizationRequest { RedirectUri = redirectUri };
        interaction.GetAuthorizationContextAsync(returnUrl, cancellation.Token).Returns(request);
        var controller = new AccountController(null!, null!, interaction, null!, null!, null!, null!)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestAborted = cancellation.Token }
            }
        };

        var result = await controller.Login(new LoginInputModel { ReturnUrl = returnUrl }, "cancel");

        await interaction.Received(1).DenyAuthenticationAsync(request, InteractionError.AccessDenied, cancellation.Token);
        if (nativeClient)
        {
            var view = (ViewResult)result;
            Assert.AreEqual("Redirect", view.ViewName);
            Assert.AreEqual(returnUrl, ((RedirectViewModel)view.Model!).RedirectUrl);
        }
        else
        {
            Assert.AreEqual(returnUrl, ((RedirectResult)result).Url);
        }
    }

    [TestMethod]
    [DataRow("WyJ3ZWJhcHAiLCJtYXVpIl0")]
    [DataRow("WyJ3ZWJhcHAiLCJtYXVpIl0=")]
    public void DiagnosticsDecodesClientList(string encoded)
    {
        var properties = new AuthenticationProperties(new Dictionary<string, string?> { ["client_list"] = encoded });
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(), properties, "test");

        var model = new DiagnosticsViewModel(AuthenticateResult.Success(ticket));

        Assert.AreSequenceEqual(new[] { "webapp", "maui" }, model.Clients);
    }
}
