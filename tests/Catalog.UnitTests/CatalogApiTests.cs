using eShop.Catalog.API;
using Microsoft.EntityFrameworkCore;
using eShop.Catalog.API.Services;
using eShop.Catalog.API.Infrastructure;
using eShop.Catalog.API.IntegrationEvents;
using eShop.Catalog.API.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace eShop.Catalog.UnitTests;

public class CatalogApiTests
{
    private readonly IConfiguration _configuration;
    private readonly DbContextOptions<CatalogContext> _dbOptions;

    public CatalogApiTests()
    {
        _configuration = new ConfigurationBuilder()
            .Build();

        _dbOptions = new DbContextOptionsBuilder<CatalogContext>()
            .UseInMemoryDatabase(databaseName: "in-memory")
            .Options;

        using var dbContext = new CatalogContext(_dbOptions, _configuration);
        dbContext.AddRange(GetFakeCatalog());
        dbContext.SaveChanges();
    }

    [Fact]
    public async Task Get_catalog_items_success()
    {
        // Arrange
        var brandFilterApplied = 1;
        var typesFilterApplied = 2;
        var pageSize = 4;
        var pageIndex = 1;

        var expectedItemsInPage = 2;
        var expectedTotalItems = 6;

        var catalogContext = new CatalogContext(_dbOptions, _configuration);
        var catalogOptions = Options.Create(new CatalogOptions()
        {
            PicBaseUrl = "http://image-server.com/"
        });

        var integrationServicesMock = new Mock<ICatalogIntegrationEventService>();
        var catalogAIMock = new Mock<ICatalogAI>();

        // Act
        var services = new CatalogServices(catalogContext, catalogAIMock.Object, catalogOptions, NullLogger<CatalogServices>.Instance, integrationServicesMock.Object);
        var actionResult = await CatalogApi.GetItemsByBrandAndTypeId(new PaginationRequest(pageSize, pageIndex), services, typesFilterApplied, brandFilterApplied);

        // Assert
        var page = Assert.IsAssignableFrom<PaginatedItems<CatalogItem>>(actionResult.Value);
        Assert.Equal(expectedTotalItems, page.Count);
        Assert.Equal(pageIndex, page.PageIndex);
        Assert.Equal(pageSize, page.PageSize);
        Assert.Equal(expectedItemsInPage, page.Data.Count());
    }

    private List<CatalogItem> GetFakeCatalog()
    {
        return new List<CatalogItem>()
        {
            new()
            {
                Id = 1,
                Name = "fakeItemA",
                CatalogTypeId = 2,
                CatalogBrandId = 1,
                PictureFileName = "fakeItemA.png"
            },
            new()
            {
                Id = 2,
                Name = "fakeItemB",
                CatalogTypeId = 2,
                CatalogBrandId = 1,
                PictureFileName = "fakeItemB.png"
            },
            new()
            {
                Id = 3,
                Name = "fakeItemC",
                CatalogTypeId = 2,
                CatalogBrandId = 1,
                PictureFileName = "fakeItemC.png"
            },
            new()
            {
                Id = 4,
                Name = "fakeItemD",
                CatalogTypeId = 2,
                CatalogBrandId = 1,
                PictureFileName = "fakeItemD.png"
            },
            new()
            {
                Id = 5,
                Name = "fakeItemE",
                CatalogTypeId = 2,
                CatalogBrandId = 1,
                PictureFileName = "fakeItemE.png"
            },
            new()
            {
                Id = 6,
                Name = "fakeItemF",
                CatalogTypeId = 2,
                CatalogBrandId = 1,
                PictureFileName = "fakeItemF.png"
            }
        };
    }
}
