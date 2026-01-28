using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Services;
using eShop.Ordering.Domain.Services.DiscountStrategies;

namespace eShop.Ordering.UnitTests.Domain;

[TestClass]
public class DiscountStrategyFactoryTests
{
    private readonly IDiscountStrategyFactory _factory;

    public DiscountStrategyFactoryTests()
    {
        _factory = new DiscountStrategyFactory();
    }

    [TestMethod]
    [DataRow(DiscountType.PercentageDiscount, typeof(PercentageDiscountStrategy))]
    [DataRow(DiscountType.FixedAmountDiscount, typeof(FixedAmountDiscountStrategy))]
    [DataRow(DiscountType.VolumeDiscount, typeof(VolumeDiscountStrategy))]
    [DataRow(DiscountType.CategoryDiscount, typeof(CategoryDiscountStrategy))]
    [DataRow(DiscountType.FirstTimeCustomerDiscount, typeof(FirstTimeCustomerDiscountStrategy))]
    public void CreateStrategy_ShouldReturnCorrectStrategyType(DiscountType type, Type expectedType)
    {
        // Act
        var strategy = _factory.CreateStrategy(type);

        // Assert
        Assert.IsInstanceOfType(strategy, expectedType);
    }
}
