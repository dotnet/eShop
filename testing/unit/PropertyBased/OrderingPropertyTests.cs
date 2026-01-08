using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.UnitTests.Shared.Generators;
using eShop.UnitTests.Shared.TestData;
using FsCheck;

namespace eShop.UnitTests.PropertyBased;

[TestClass]
[TestCategory("Property")]
public class OrderingPropertyTests
{
    [TestMethod]
    public void Order_AddOrderItem_TotalAlwaysIncreases()
    {
        // **Feature: ordering-management, Property 1: Order Total Calculation**
        // **Validates: Requirements ORD-1.1, ORD-1.2**
        
        Prop.ForAll(
            PropertyTestGenerators.ValidOrderItems(),
            orderItems =>
            {
                // Arrange
                var order = OrderTestData.CreateValidOrder();
                var initialTotal = order.GetTotal();

                // Act
                foreach (var item in orderItems)
                {
                    order.AddOrderItem(
                        item.ProductId,
                        item.ProductName,
                        item.UnitPrice,
                        item.Discount,
                        item.PictureUrl,
                        item.Units
                    );
                }

                // Assert - Total should increase (or stay same if no items)
                var finalTotal = order.GetTotal();
                return finalTotal >= initialTotal;
            })
            .QuickCheckThrowOnFailure();
    }

    [TestMethod]
    public void Order_AddSameProductTwice_QuantityAccumulates()
    {
        // **Feature: ordering-management, Property 2: Product Quantity Accumulation**
        // **Validates: Requirements ORD-2.1**
        
        Prop.ForAll(
            Gen.Choose(1, 1000), // Product ID
            Gen.Choose(1, 100),  // First quantity
            Gen.Choose(1, 100),  // Second quantity
            PropertyTestGenerators.ValidProductNames(),
            PropertyTestGenerators.ValidPrices(),
            (productId, quantity1, quantity2, productName, unitPrice) =>
            {
                // Arrange
                var order = OrderTestData.CreateValidOrder();

                // Act
                order.AddOrderItem(productId, productName, unitPrice, 0, "test.jpg", quantity1);
                order.AddOrderItem(productId, productName, unitPrice, 0, "test.jpg", quantity2);

                // Assert - Should have only one item with combined quantity
                var orderItem = order.OrderItems.Single(i => i.ProductId == productId);
                return orderItem.Units == quantity1 + quantity2;
            })
            .QuickCheckThrowOnFailure();
    }

    [TestMethod]
    public void Order_StatusTransitions_FollowValidSequence()
    {
        // **Feature: ordering-management, Property 3: Order Status State Machine**
        // **Validates: Requirements ORD-3.1, ORD-3.2**
        
        Prop.ForAll(
            Gen.Elements(OrderStatus.Submitted, OrderStatus.AwaitingValidation, OrderStatus.StockConfirmed),
            initialStatus =>
            {
                // Arrange
                var order = OrderTestData.CreateValidOrder();
                
                // Set initial status using reflection (since constructor sets to Submitted)
                var statusField = typeof(Order).GetField("_orderStatusId", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                statusField?.SetValue(order, (int)initialStatus);

                // Act & Assert - Valid transitions should always work
                try
                {
                    switch (initialStatus)
                    {
                        case OrderStatus.Submitted:
                            order.SetAwaitingValidationStatus();
                            return order.OrderStatus == OrderStatus.AwaitingValidation;
                            
                        case OrderStatus.AwaitingValidation:
                            order.SetStockConfirmedStatus();
                            return order.OrderStatus == OrderStatus.StockConfirmed;
                            
                        case OrderStatus.StockConfirmed:
                            order.SetPaidStatus();
                            return order.OrderStatus == OrderStatus.Paid;
                            
                        default:
                            return true; // Other statuses are valid as-is
                    }
                }
                catch
                {
                    return false; // Invalid transition
                }
            })
            .QuickCheckThrowOnFailure();
    }

    [TestMethod]
    public void Order_CalculateTotal_IsCommutative()
    {
        // **Feature: ordering-management, Property 4: Order Total Commutativity**
        // **Validates: Requirements ORD-1.3**
        
        Prop.ForAll(
            PropertyTestGenerators.ValidOrderItems().Where(items => items.Count >= 2),
            orderItems =>
            {
                // Arrange
                var order1 = OrderTestData.CreateValidOrder();
                var order2 = OrderTestData.CreateValidOrder();

                // Act - Add items in different orders
                foreach (var item in orderItems)
                {
                    order1.AddOrderItem(item.ProductId, item.ProductName, item.UnitPrice, 
                                      item.Discount, item.PictureUrl, item.Units);
                }

                foreach (var item in orderItems.Reverse())
                {
                    order2.AddOrderItem(item.ProductId, item.ProductName, item.UnitPrice, 
                                      item.Discount, item.PictureUrl, item.Units);
                }

                // Assert - Total should be the same regardless of order
                return order1.GetTotal() == order2.GetTotal();
            })
            .QuickCheckThrowOnFailure();
    }

    [TestMethod]
    public void Order_ApplyDiscount_NeverExceedsItemPrice()
    {
        // **Feature: ordering-management, Property 5: Discount Validation**
        // **Validates: Requirements ORD-4.1**
        
        Prop.ForAll(
            Gen.Choose(1, 1000),
            PropertyTestGenerators.ValidProductNames(),
            PropertyTestGenerators.ValidPrices(),
            Gen.Choose(0m, 1000m), // Discount amount
            Gen.Choose(1, 10),     // Quantity
            (productId, productName, unitPrice, discount, quantity) =>
            {
                // Arrange
                var order = OrderTestData.CreateValidOrder();

                // Act
                order.AddOrderItem(productId, productName, unitPrice, discount, "test.jpg", quantity);

                // Assert - Item total should never be negative
                var orderItem = order.OrderItems.First(i => i.ProductId == productId);
                var itemTotal = (orderItem.UnitPrice - orderItem.Discount) * orderItem.Units;
                return itemTotal >= 0;
            })
            .QuickCheckThrowOnFailure();
    }

    [TestMethod]
    public void Order_CancelOrder_PreservesOrderItems()
    {
        // **Feature: ordering-management, Property 6: Order Cancellation Invariants**
        // **Validates: Requirements ORD-5.1**
        
        Prop.ForAll(
            PropertyTestGenerators.ValidOrderItems(),
            orderItems =>
            {
                // Arrange
                var order = OrderTestData.CreateValidOrder();
                foreach (var item in orderItems)
                {
                    order.AddOrderItem(item.ProductId, item.ProductName, item.UnitPrice, 
                                     item.Discount, item.PictureUrl, item.Units);
                }
                var originalItemCount = order.OrderItems.Count;

                // Act
                order.SetCancelledStatus();

                // Assert - Cancellation should preserve order items
                return order.OrderItems.Count == originalItemCount && 
                       order.OrderStatus == OrderStatus.Cancelled;
            })
            .QuickCheckThrowOnFailure();
    }

    [TestMethod]
    public void Address_CreateAddress_AllFieldsPreserved()
    {
        // **Feature: ordering-management, Property 7: Address Value Object Immutability**
        // **Validates: Requirements ORD-6.1**
        
        Prop.ForAll(
            PropertyTestGenerators.ValidStreetAddresses(),
            PropertyTestGenerators.ValidCityNames(),
            PropertyTestGenerators.ValidStateNames(),
            PropertyTestGenerators.ValidCountryNames(),
            PropertyTestGenerators.ValidZipCodes(),
            (street, city, state, country, zipCode) =>
            {
                // Act
                var address = new Address(street, city, state, country, zipCode);

                // Assert - All fields should be preserved exactly
                return address.Street == street &&
                       address.City == city &&
                       address.State == state &&
                       address.Country == country &&
                       address.ZipCode == zipCode;
            })
            .QuickCheckThrowOnFailure();
    }

    [TestMethod]
    public void Order_MultipleItemsWithSameProduct_MaintainsSingleEntry()
    {
        // **Feature: ordering-management, Property 8: Product Consolidation**
        // **Validates: Requirements ORD-2.2**
        
        Prop.ForAll(
            Gen.Choose(1, 100), // Product ID
            PropertyTestGenerators.ValidProductNames(),
            PropertyTestGenerators.ValidPrices(),
            Gen.ListOf(Gen.Choose(1, 10)).Where(quantities => quantities.Count > 1), // Multiple quantities
            (productId, productName, unitPrice, quantities) =>
            {
                // Arrange
                var order = OrderTestData.CreateValidOrder();

                // Act - Add same product multiple times
                foreach (var quantity in quantities)
                {
                    order.AddOrderItem(productId, productName, unitPrice, 0, "test.jpg", quantity);
                }

                // Assert - Should have only one entry for the product
                var productItems = order.OrderItems.Where(i => i.ProductId == productId).ToList();
                var expectedTotalQuantity = quantities.Sum();
                
                return productItems.Count == 1 && 
                       productItems.First().Units == expectedTotalQuantity;
            })
            .QuickCheckThrowOnFailure();
    }
}