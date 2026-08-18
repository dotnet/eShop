using eShop.Catalog.API.Model;
using eShop.Catalog.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace eShop.Application.UnitTests;

[TestClass]
public class CatalogAITests
{
    [TestMethod]
    public async Task DisabledCatalogAIReturnsNullForEveryEmbeddingOperation()
    {
        var environment = Substitute.For<IWebHostEnvironment>();
        var catalogAI = new CatalogAI(environment, NullLogger<CatalogAI>.Instance);
        var item = new CatalogItem("Test") { Description = "Description" };

        Assert.IsFalse(catalogAI.IsEnabled);
        Assert.IsNull(await catalogAI.GetEmbeddingAsync("search"));
        Assert.IsNull(await catalogAI.GetEmbeddingAsync(item));
        Assert.IsNull(await catalogAI.GetEmbeddingsAsync([item]));
    }
}
