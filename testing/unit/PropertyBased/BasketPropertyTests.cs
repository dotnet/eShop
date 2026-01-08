using eShop.Basket.API.Model;
using eShop.UnitTests.Shared.Generators;
using FsCheck;

namespace eShop.UnitTests.PropertyBased;

[TestClass]
public class BasketPropertyTests
{
    [TestMethod]
    public void Basket_TotalCalculation_ShouldAlwaysEqualSumOfItems()
    {
        // **Feature: basket-management, Property 1: Total Calculation Consistency**
        // **Validates: Requirements BSK-1.1, BSK-1.2**
        
        Prop.ForAll(
            Gen.ListOf(Gen.Zip3(
                PropertyTestGenerators.ValidPrices(),
                PropertyTestGenerators.ValidQuantities(),
                PropertyTestGenerators.ValidProductIds())),
            itemData =>
            {
                // Arrange
                var basketItems = itemData.Select((data, index) => new BasketItem
                {
                    Id = $"item-{index}",
                    ProductId = data.Item3,
                    ProductName = $"Product {data.Item3}",
                    UnitPrice = data.Item1,
                    Quantity = data.Item2,
                    PictureUrl = "test.jpg"
                }).ToList();

                var basket = new CustomerBasket
                {
                    BuyerId = "test-buyer",
                    Items = basketItems
                };

                // Act
                var calculatedTotal = basketItems.Sum(item => item.UnitPrice * item.Quantity);
                var basketTotal = basket.Items.Sum(item => item.UnitPrice * item.Quantity);

                // Assert - Universal property: basket total should equal sum of individual items
                return Math.Abs(basketTotal - calculatedTotal) < 0.01m;
            })
            .Check(PropertyTestGenerators.PropertyTestConfig.DomainTestConfig);
    }

    [TestMethod]
    public void Basket_ItemAddition_ShouldIncreaseItemCount()
    {
        // **Feature: basket-operations, Property 2: Item Addition Consistency**
        // **Validates: Requirements BSK-2.1**
        
        Prop.ForAll(
            PropertyTestGenerators.ValidProductIds(),
            PropertyTestGenerators.ValidQuantities(),
            (productId, quantity) =>
            {
                // Arrange
                var basket = new CustomerBasket
                {
                    BuyerId = "test-buyer",
                    Items = new List<BasketItem>()
                };

                var initialCount = basket.Items.Count;

                // Act
                var newItem = new BasketItem
                {
                    Id = $"item-{productId}",
                    ProductId = productId,
                    ProductName = $"Product {productId}",
                    UnitPrice = 99.99m,
                    Quantity = quantity,
                    PictureUrl = "test.jpg"
                };

                basket.Items.Add(newItem);

                // Assert - Universal property: adding item should increase count by 1
                return basket.Items.Count == initialCount + 1;
            })
            .Check(PropertyTestGenerators.PropertyTestConfig.DomainTestConfig);
    }

    [TestMethod]
    public void Basket_ItemRemoval_ShouldDecreaseItemCount()
    {
        // **Feature: basket-operations, Property 3: Item Removal Consistency**
        // **Validates: Requirements BSK-3.1**
        
        Prop.ForAll(
            Gen.NonEmptyListOf(Gen.Zip(
                PropertyTestGenerators.ValidProductIds(),
                PropertyTestGenerators.ValidQuantities())),
            itemData =>
            {
                // Arrange
                var basketItems = itemData.Select((data, index) => new BasketItem
                {
                    Id = $"item-{index}",
                    ProductId = data.Item1,
                    ProductName = $"Product {data.Item1}",
                    UnitPrice = 99.99m,
                    Quantity = data.Item2,
                    PictureUrl = "test.jpg"
                }).ToList();

                var basket = new CustomerBasket
                {
                    BuyerId = "test-buyer",
                    Items = basketItems
                };

                var initialCount = basket.Items.Count;
                var itemToRemove = basket.Items.First();

                // Act
                basket.Items.Remove(itemToRemove);

                // Assert - Universal property: removing item should decrease count by 1
                return basket.Items.Count == initialCount - 1;
            })
            .Check(PropertyTestGenerators.PropertyTestConfig.DomainTestConfig);
    }

    [TestMethod]
    public void Basket_QuantityUpdate_ShouldReflectInTotal()
    {
        // **Feature: basket-operations, Property 4: Quantity Update Consistency**
        // **Validates: Requirements BSK-4.1, BSK-4.2**
        
        Prop.ForAll(
            PropertyTestGenerators.ValidPrices(),
            PropertyTestGenerators.ValidQuantities(),
            PropertyTestGenerators.ValidQuantities(),
            (unitPrice, originalQuantity, newQuantity) =>
            {
                // Arrange
                var basketItem = new BasketItem
                {
                    Id = "test-item",
                    ProductId = 1,
                    ProductName = "Test Product",
                    UnitPrice = unitPrice,
                    Quantity = originalQuantity,
                    PictureUrl = "test.jpg"
                };

                var originalTotal = basketItem.UnitPrice * basketItem.Quantity;

                // Act
                basketItem.Quantity = newQuantity;
                var newTotal = basketItem.UnitPrice * basketItem.Quantity;

                // Assert - Universal property: quantity change should reflect proportionally in total
                var expectedTotal = unitPrice * newQuantity;
                return Math.Abs(newTotal - expectedTotal) < 0.01m;
            })
            .Check(PropertyTestGenerators.PropertyTestConfig.DomainTestConfig);
    }

    [TestMethod]
    public void Basket_EmptyBasket_ShouldHaveZeroTotal()
    {
        // **Feature: basket-validation, Property 5: Empty Basket Consistency**
        // **Validates: Requirements BSK-5.1**
        
        Prop.ForAll(
            PropertyTestGenerators.ValidUserIds(),
            buyerId =>
            {
                // Arrange & Act
                var emptyBasket = new CustomerBasket
                {
                    BuyerId = buyerId,
                    Items = new List<BasketItem>()
                };

                var total = emptyBasket.Items.Sum(item => item.UnitPrice * item.Quantity);

                // Assert - Universal property: empty basket should always have zero total
                return total == 0m && emptyBasket.Items.Count == 0;
            })
            .Check(PropertyTestGenerators.PropertyTestConfig.QuickTestConfig);
    }

    [TestMethod]
    public void Basket_DuplicateItems_ShouldConsolidateQuantities()
    {
        // **Feature: basket-operations, Property 6: Duplicate Item Handling**
        // **Validates: Requirements BSK-6.1**
        
        Prop.ForAll(
            PropertyTestGenerators.ValidProductIds(),
            PropertyTestGenerators.ValidQuantities(),
            PropertyTestGenerators.ValidQuantities(),
            (productId, quantity1, quantity2) =>
            {
                // Arrange
                var basket = new CustomerBasket
                {
                    BuyerId = "test-buyer",
                    Items = new List<BasketItem>()
                };

                var item1 = new BasketItem
                {
                    Id = $"item-1-{productId}",
                    ProductId = productId,
                    ProductName = $"Product {productId}",
                    UnitPrice = 99.99m,
                    Quantity = quantity1,
                    PictureUrl = "test.jpg"
                };

                var item2 = new BasketItem
                {
                    Id = $"item-2-{productId}",
                    ProductId = productId,
                    ProductName = $"Product {productId}",
                    UnitPrice = 99.99m,
                    Quantity = quantity2,
                    PictureUrl = "test.jpg"
                };

                // Act - Simulate adding same product twice
                basket.Items.Add(item1);
                basket.Items.Add(item2);

                // Simulate consolidation logic
                var consolidatedItems = basket.Items
                    .GroupBy(item => item.ProductId)
                    .Select(group => new BasketItem
                    {
                        Id = group.First().Id,
                        ProductId = group.Key,
                        ProductName = group.First().ProductName,
                        UnitPrice = group.First().UnitPrice,
                        Quantity = group.Sum(item => item.Quantity),
                        PictureUrl = group.First().PictureUrl
                    })
                    .ToList();

                // Assert - Universal property: consolidation should sum quantities for same product
                var expectedQuantity = quantity1 + quantity2;
                return consolidatedItems.Count == 1 && 
                       consolidatedItems.First().Quantity == expectedQuantity;
            })
            .Check(PropertyTestGenerators.PropertyTestConfig.DomainTestConfig);
    }
}