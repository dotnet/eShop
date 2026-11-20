namespace eShop.Ordering.UnitTests.Domain;

[TestClass]
public class BuyerAggregateTest
{
    public BuyerAggregateTest()
    { }

    [TestMethod]
    public void Create_buyer_item_success()
    {
        //Arrange    
        var identity = new Guid().ToString();
        var name = "fakeUser";

        //Act 
        var fakeBuyerItem = new Buyer(identity, name);

        //Assert
        Assert.IsNotNull(fakeBuyerItem);
    }

    [TestMethod]
    public void Create_buyer_item_fail()
    {
        //Arrange    
        var identity = string.Empty;
        var name = "fakeUser";

        //Act - Assert
        Assert.ThrowsExactly<ArgumentNullException>(() => new Buyer(identity, name));
    }

    [TestMethod]
    public void add_payment_success()
    {
        //Arrange    
        var alias = "fakeAlias";
        var paymentToken = "pm_test_123";
        var orderId = 1;
        var name = "fakeUser";
        var identity = new Guid().ToString();
        var fakeBuyerItem = new Buyer(identity, name);

        //Act
        var result = fakeBuyerItem.VerifyOrAddPaymentMethod(alias, paymentToken, orderId);

        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void create_payment_method_success()
    {
        //Arrange    
        var alias = "fakeAlias";
        var paymentToken = "pm_test_123";

        //Act
        var result = new PaymentMethod(alias, paymentToken);

        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void create_payment_method_empty_reference_fail()
    {
        //Arrange    
        var alias = "fakeAlias";
        var paymentToken = string.Empty;

        //Act - Assert
        Assert.ThrowsExactly<OrderingDomainException>(() => new PaymentMethod(alias, paymentToken));
    }

    [TestMethod]
    public void payment_method_isEqualTo()
    {
        //Arrange    
        var alias = "fakeAlias";
        var paymentToken = "pm_test_123";

        //Act
        var fakePaymentMethod = new PaymentMethod(alias, paymentToken);
        var result = fakePaymentMethod.IsEqualTo(paymentToken);

        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Add_new_PaymentMethod_raises_new_event()
    {
        //Arrange    
        var alias = "fakeAlias";
        var orderId = 1;
        var paymentToken = "pm_test_123";
        var expectedResult = 1;
        var name = "fakeUser";

        //Act 
        var fakeBuyer = new Buyer(Guid.NewGuid().ToString(), name);
        fakeBuyer.VerifyOrAddPaymentMethod(alias, paymentToken, orderId);

        //Assert
        Assert.HasCount(expectedResult, fakeBuyer.DomainEvents);
    }
}
