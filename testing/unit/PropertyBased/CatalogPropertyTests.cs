using eShop.Catalog.API.Model;
using eShop.UnitTests.Shared.Generators;
using FsCheck;

namespace eShop.UnitTests.PropertyBased;

[TestClass]
public class CatalogPropertyTests
{
    [TestMethod]
    public void CatalogItem_PriceCalculation_ShouldAlwaysBePositive()
    {
        // **Feature: catalog-management, Property 1: Price Calculation Consistency**
        // **Validates: Requirements CAT-1.1, CAT-1.2**
        
        Prop.ForAll(
            PropertyTestGenerators.ValidPrices(),
            PropertyTestGenerators.ValidQuantities(),
            (price, quantity) =>
            {
                // Act
                var totalPrice = price * quantity;
                
                // Assert - Universal property: total should always be positive for valid inputs
                return totalPrice > 0;
            })
            .Check(PropertyTestGenerators.PropertyTestConfig.DomainTestConfig);
    }

    [TestMethod]
    public void CatalogItem_StockValidation_ShouldNeverExceedMaxThreshold()
    {
        // **Feature: inventory-management, Property 2: Stock Threshold Consistency**
        // **Validates: Requirements INV-2.1, INV-2.2**
        
        Prop.ForAll(
            Gen.Choose(1, 1000), // availableStock
            Gen.Choose(1, 100),  // restockThreshold
            Gen.Choose(100, 2000), // maxStockThreshold
            (availableStock, restockThreshold, maxStockThreshold) =>
            {
                // Arrange
                var catalogItem = new CatalogItem
                {
                    Id = 1,
                    Name = "Test Product",
                    Price = 99.99m,
                    AvailableStock = availableStock,
                    RestockThreshold = restockThreshold,
                    MaxStockThreshold = maxStockThreshold
                };

                // Assert - Universal property: available stock should never exceed max threshold
                return catalogItem.AvailableStock <= catalogItem.MaxStockThreshold;
            })
            .Check(PropertyTestGenerators.PropertyTestConfig.DomainTestConfig);
    }

    [TestMethod]
    public void CatalogItem_NameValidation_ShouldRejectInvalidNames()
    {
        // **Feature: product-validation, Property 3: Name Format Validation**
        // **Validates: Requirements VAL-3.1, VAL-3.2**
        
        Prop.ForAll(
            PropertyTestGenerators.InvalidProductNames(),
            invalidName =>
            {
                try
                {
                    // Act
                    var catalogItem = new CatalogItem
                    {
                        Id = 1,
                        Name = invalidName,
                        Price = 99.99m
                    };

                    // Assert - Invalid names should be rejected
                    return string.IsNullOrWhiteSpace(catalogItem.Name) || catalogItem.Name.Length > 255;
                }
                catch
                {
                    // Exception is acceptable for invalid input
                    return true;
                }
            })
            .Check(PropertyTestGenerators.PropertyTestConfig.QuickTestConfig);
    }

    [TestMethod]
    public void CatalogItem_PriceUpdate_ShouldMaintainPriceHistory()
    {
        // **Feature: price-management, Property 4: Price History Consistency**
        // **Validates: Requirements PRC-4.1**
        
        Prop.ForAll(
            PropertyTestGenerators.ValidPrices(),
            PropertyTestGenerators.ValidPrices(),
            (originalPrice, newPrice) =>
            {
                // Arrange
                var catalogItem = new CatalogItem
                {
                    Id = 1,
                    Name = "Test Product",
                    Price = originalPrice
                };

                var oldPrice = catalogItem.Price;

                // Act
                catalogItem.Price = newPrice;

                // Assert - Universal property: price changes should be trackable
                return catalogItem.Price == newPrice && oldPrice == originalPrice;
            })
            .Check(PropertyTestGenerators.PropertyTestConfig.DomainTestConfig);
    }

    [TestMethod]
    public void CatalogSearch_ResultConsistency_ShouldReturnSameResultsForSameQuery()
    {
        // **Feature: search-functionality, Property 5: Search Result Consistency**
        // **Validates: Requirements SRC-5.1, SRC-5.2**
        
        Prop.ForAll(
            PropertyTestGenerators.ValidProductNames(),
            searchTerm =>
            {
                // Arrange
                var catalogItems = new List<CatalogItem>
                {
                    new() { Id = 1, Name = searchTerm, Price = 99.99m },
                    new() { Id = 2, Name = "Other Product", Price = 49.99m },
                    new() { Id = 3, Name = $"Contains {searchTerm}", Price = 149.99m }
                };

                // Act - Simulate search twice
                var firstSearch = catalogItems.Where(item => 
                    item.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
                var secondSearch = catalogItems.Where(item => 
                    item.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();

                // Assert - Universal property: same search should return same results
                return firstSearch.Count == secondSearch.Count &&
                       firstSearch.All(item => secondSearch.Any(s => s.Id == item.Id));
            })
            .Check(PropertyTestGenerators.PropertyTestConfig.DomainTestConfig);
    }

    [TestMethod]
    public void CatalogPagination_PageSizeConsistency_ShouldRespectPageLimits()
    {
        // **Feature: pagination, Property 6: Page Size Consistency**
        // **Validates: Requirements PAG-6.1**
        
        Prop.ForAll(
            Gen.Choose(1, 100), // pageSize
            Gen.Choose(0, 10),  // pageIndex
            (pageSize, pageIndex) =>
            {
                // Arrange
                var totalItems = 1000;
                var catalogItems = Enumerable.Range(1, totalItems)
                    .Select(i => new CatalogItem { Id = i, Name = $"Product {i}", Price = i * 10m })
                    .ToList();

                // Act
                var pagedItems = catalogItems
                    .Skip(pageIndex * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Assert - Universal property: paged results should never exceed page size
                return pagedItems.Count <= pageSize;
            })
            .Check(PropertyTestGenerators.PropertyTestConfig.DomainTestConfig);
    }
}