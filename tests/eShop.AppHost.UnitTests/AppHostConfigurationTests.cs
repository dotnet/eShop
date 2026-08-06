using Aspire.Hosting;
using eShop.AppHost;
using Microsoft.Extensions.Configuration;

namespace eShop.AppHost.UnitTests;

[TestClass]
public class AppHostConfigurationTests
{
    [TestMethod]
    [DataRow(null, false)]
    [DataRow("", false)]
    [DataRow("invalid", false)]
    [DataRow("false", false)]
    [DataRow("true", true)]
    public void FoundryFlagUsesSafeOptInDefault(string? configuredValue, bool expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["UseFoundry"] = configuredValue })
            .Build();

        Assert.AreEqual(expected, Extensions.IsFoundryEnabled(configuration));
    }

    [TestMethod]
    public void FoundryExtensionAddsExpectedDeployments()
    {
        var builder = CreateBuilder();
        var catalog = builder.AddProject("catalog-api", ProjectPath("Catalog.API", "Catalog.API.csproj"));
        var webApp = builder.AddProject("webapp", ProjectPath("WebApp", "WebApp.csproj"));

        builder.AddFoundry(catalog, webApp);

        CollectionAssert.IsSubsetOf(
            new[] { "foundry", "chatModel", "textEmbeddingModel" },
            builder.Resources.Select(resource => resource.Name).ToArray());
    }

    [TestMethod]
    public void OllamaExtensionAddsExpectedModels()
    {
        var builder = CreateBuilder();
        var catalog = builder.AddProject("catalog-api", ProjectPath("Catalog.API", "Catalog.API.csproj"));
        var webApp = builder.AddProject("webapp", ProjectPath("WebApp", "WebApp.csproj"));

        builder.AddOllama(catalog, webApp);

        CollectionAssert.IsSubsetOf(
            new[] { "ollama", "embedding", "chat" },
            builder.Resources.Select(resource => resource.Name).ToArray());
    }

    private static IDistributedApplicationBuilder CreateBuilder() =>
        DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            AssemblyName = typeof(AppHostConfigurationTests).Assembly.FullName,
            DisableDashboard = true
        });

    private static string ProjectPath(string directory, string project) =>
        Path.Combine(FindRepositoryRoot(), "src", directory, project);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "eShop.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the eShop repository root.");
    }
}
