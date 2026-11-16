namespace eShop.Ordering.UnitTests.Domain;

using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.UnitTests.Domain;

[TestClass]
public class OrderAggregateTest
{
    public OrderAggregateTest()
    { }

    [TestMethod]
    public void Create_order_item_success()
    {
        //Arrange    
        var productId = 1;
        var productName = "FakeProductName";
        var unitPrice = 12;
        var discount = 15;
        var pictureUrl = "FakeUrl";
        var units = 5;

        //Act 
        var fakeOrderItem = new OrderItem(productId, productName, unitPrice, discount, pictureUrl, units);

        //Assert
        Assert.IsNotNull(fakeOrderItem);
    }

    [TestMethod]
    public void Invalid_number_of_units()
    {
        //Arrange    
        var productId = 1;
        var productName = "FakeProductName";
        var unitPrice = 12;
        var discount = 15;
        var pictureUrl = "FakeUrl";
        var units = -1;

        //Act - Assert
        Assert.ThrowsExactly<OrderingDomainException>(() => new OrderItem(productId, productName, unitPrice, discount, pictureUrl, units));
    }

    [TestMethod]
    public void Invalid_total_of_order_item_lower_than_discount_applied()
    {
        //Arrange    
        var productId = 1;
        var productName = "FakeProductName";
        var unitPrice = 12;
        var discount = 15;
        var pictureUrl = "FakeUrl";
        var units = 1;
        
        //Act - Assert
        Assert.ThrowsExactly<OrderingDomainException>(() => new OrderItem(productId, productName, unitPrice, discount, pictureUrl, units));       
    }

    [TestMethod]
    public void Invalid_discount_setting()
    {
        //Arrange    
        var productId = 1;
        var productName = "FakeProductName";
        var unitPrice = 12;
        var discount = 15;
        var pictureUrl = "FakeUrl";
        var units = 5;

        //Act 
        var fakeOrderItem = new OrderItem(productId, productName, unitPrice, discount, pictureUrl, units);

        //Assert
        Assert.ThrowsExactly<OrderingDomainException>(() => fakeOrderItem.SetNewDiscount(-1));
    }

    [TestMethod]
    public void Invalid_units_setting()
    {
        //Arrange    
        var productId = 1;
        var productName = "FakeProductName";
        var unitPrice = 12;
        var discount = 15;
        var pictureUrl = "FakeUrl";
        var units = 5;

        //Act 
        var fakeOrderItem = new OrderItem(productId, productName, unitPrice, discount, pictureUrl, units);

        //Assert
        Assert.ThrowsExactly<OrderingDomainException>(() => fakeOrderItem.AddUnits(-1));
    }

    [TestMethod]
    public void when_add_two_times_on_the_same_item_then_the_total_of_order_should_be_the_sum_of_the_two_items()
    {
        var address = new AddressBuilder().Build();
        var order = new OrderBuilder(address)
            .AddOne(1, "cup", 10.0m, 0, string.Empty)
            .AddOne(1, "cup", 10.0m, 0, string.Empty)
            .Build();

        Assert.AreEqual(20.0m, order.GetTotal());
    }

    [TestMethod]
    public void New_order_is_submitted()
    {
        var address = new AddressBuilder().Build();
        var order = new Order(address);
        Assert.AreEqual(OrderStatus.Submitted, order.OrderStatus);
        Assert.AreSame(address, order.Address);
    }

    [TestMethod]
    public void Order_status_transitions_keep_their_preconditions()
    {
        var order = new Order(new AddressBuilder().Build());
        order.SetPaidStatus();
        order.SetStockConfirmedStatus();
        Assert.AreEqual(OrderStatus.Submitted, order.OrderStatus);
        Assert.ThrowsExactly<OrderingDomainException>(() => order.SetShippedStatus());

        order.SetAwaitingValidationStatus();
        order.SetStockConfirmedStatus();
        order.SetPaidStatus();
        Assert.AreEqual(OrderStatus.Paid, order.OrderStatus);
        Assert.ThrowsExactly<OrderingDomainException>(() => order.SetCancelledStatus());
        order.SetShippedStatus();
        Assert.AreEqual(OrderStatus.Shipped, order.OrderStatus);
    }

    [TestMethod]
    public void Rejected_stock_cancels_only_awaiting_validation_orders()
    {
        var order = new OrderBuilder(new AddressBuilder().Build()).AddOne(1, "Mug", 10, 0, "").Build();
        order.SetCancelledStatusWhenStockIsRejected([1]);
        Assert.AreEqual(OrderStatus.Submitted, order.OrderStatus);
        order.SetAwaitingValidationStatus();
        order.SetCancelledStatusWhenStockIsRejected([1]);
        Assert.AreEqual(OrderStatus.Cancelled, order.OrderStatus);
        Assert.Contains("Mug", order.Description);
    }
}
