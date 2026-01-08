using eShop.Catalog.API.Services;
using eShop.Catalog.API.Model;
using eShop.Catalog.API.Infrastructure;
using eShop.UnitTests.Shared.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eShop.UnitTests.Services.Catalog;

[TestClass]
public class CatalogServiceTests
{
    private ICatalogService _catalogService;
    private CatalogContext _mockContext;
    private ILogger<CatalogService> _mockLogger;

    [TestInitialize]
    public void Setup()
    {
        _mockContext = Substitute.For<CatalogContext>();
        _mockLogger = Substitute.For<ILogger<CatalogService>>();
        _catalogService = new CatalogService(_mockContext, _mockLogger);
    }

    [TestMethod]
    public async Task GetCatalogItems_ValidPageSize_ReturnsCorrectCount()
    {
        // Arrange
        var catalogItems = ProductTestData.CreateCatalogItemList(10);
        var mockDbSet = CreateMockDbSet(catalogItems);
        _mockContext.CatalogItems.Returns(mockDbSet);

        // Act
        var result = await _catalogService.GetCatalogItemsAsync(0, 5);

        // Assert
        result.ShouldNotBeNull();
        result.Data.Count().ShouldBe(5);
        result.TotalCount.ShouldBe(10);
    }

    [TestMethod]
    public async Task GetCatalogItemById_ValidId_ReturnsItem()
    {
        // Arrange
        var catalogItem = ProductTestData.CreateValidCatalogItem(id: 1);
        var catalogItems = new List<CatalogItem> { catalogItem };
        var mockDbSet = CreateMockDbSet(catalogItems);
        _mockContext.CatalogItems.Returns(mockDbSet);

        // Act
        var result = await _catalogService.GetCatalogItemAsync(1);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Name.ShouldBe(catalogItem.Name);
    }

    [TestMethod]
    public async Task GetCatalogItemById_InvalidId_ReturnsNull()
    {
        // Arrange
        var catalogItems = ProductTestData.CreateCatalogItemList(5);
        var mockDbSet = CreateMockDbSet(catalogItems);
        _mockContext.CatalogItems.Returns(mockDbSet);

        // Act
        var result = await _catalogService.GetCatalogItemAsync(999);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetCatalogItemsByBrand_ValidBrandId_ReturnsFilteredItems()
    {
        // Arrange
        var catalogItems = ProductTestData.CreateCatalogItemList(10);
        // Set specific brand IDs
        catalogItems[0].CatalogBrandId = 1;
        catalogItems[1].CatalogBrandId = 1;
        catalogItems[2].CatalogBrandId = 2;

        var mockDbSet = CreateMockDbSet(catalogItems);
        _mockContext.CatalogItems.Returns(mockDbSet);

        // Act
        var result = await _catalogService.GetCatalogItemsByBrandAsync(1, 0, 10);

        // Assert
        result.ShouldNotBeNull();
        result.Data.All(item => item.CatalogBrandId == 1).ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetCatalogItemsByType_ValidTypeId_ReturnsFilteredItems()
    {
        // Arrange
        var catalogItems = ProductTestData.CreateCatalogItemList(10);
        // Set specific type IDs
        catalogItems[0].CatalogTypeId = 1;
        catalogItems[1].CatalogTypeId = 1;
        catalogItems[2].CatalogTypeId = 2;

        var mockDbSet = CreateMockDbSet(catalogItems);
        _mockContext.CatalogItems.Returns(mockDbSet);

        // Act
        var result = await _catalogService.GetCatalogItemsByTypeAsync(1, 0, 10);

        // Assert
        result.ShouldNotBeNull();
        result.Data.All(item => item.CatalogTypeId == 1).ShouldBeTrue();
    }

    [TestMethod]
    public async Task UpdateCatalogItem_ValidItem_UpdatesSuccessfully()
    {
        // Arrange
        var catalogItem = ProductTestData.CreateValidCatalogItem(id: 1);
        var catalogItems = new List<CatalogItem> { catalogItem };
        var mockDbSet = CreateMockDbSet(catalogItems);
        _mockContext.CatalogItems.Returns(mockDbSet);

        var updatedItem = ProductTestData.CreateValidCatalogItem(
            id: 1,
            name: "Updated Product",
            price: 199.99m
        );

        // Act
        await _catalogService.UpdateCatalogItemAsync(updatedItem);

        // Assert
        _mockContext.Received(1).SaveChangesAsync();
        catalogItem.Name.ShouldBe("Updated Product");
        catalogItem.Price.ShouldBe(199.99m);
    }

    private static DbSet<T> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockDbSet = Substitute.For<DbSet<T>, IQueryable<T>>();
        
        ((IQueryable<T>)mockDbSet).Provider.Returns(queryable.Provider);
        ((IQueryable<T>)mockDbSet).Expression.Returns(queryable.Expression);
        ((IQueryable<T>)mockDbSet).ElementType.Returns(queryable.ElementType);
        ((IQueryable<T>)mockDbSet).GetEnumerator().Returns(queryable.GetEnumerator());

        return mockDbSet;
    }
}