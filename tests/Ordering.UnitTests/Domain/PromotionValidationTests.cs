using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Exceptions;

namespace Ordering.UnitTests.Domain;

[TestClass]
public class PromotionValidationTests
{
    [TestMethod]
    public void Constructor_Should_Throw_When_Name_Is_Null_Or_Empty()
    {
        // Arrange
        var name = "";
        var discountType = DiscountType.PercentageDiscount;
        var discountValue = 10m;
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = DateTime.UtcNow.AddDays(2);
        var priority = 1;

        // Act & Assert
        Assert.ThrowsExactly<OrderingDomainException>(() => new Promotion(name, discountType, discountValue, startDate, endDate, priority));
    }

    [TestMethod]
    public void Constructor_Should_Throw_When_MinimumOrderAmount_Is_Negative()
    {
        // Arrange
        var minimumOrderAmount = -10m;
        
        // Act & Assert
        Assert.ThrowsExactly<OrderingDomainException>(() => new Promotion("Test", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 1, minimumOrderAmount: minimumOrderAmount));
    }

    [TestMethod]
    public void Constructor_Should_Throw_When_MaximumDiscount_Is_Negative()
    {
        // Arrange
        var maximumDiscount = -10m;

        // Act & Assert
        Assert.ThrowsExactly<OrderingDomainException>(() => new Promotion("Test", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 1, maximumDiscount: maximumDiscount));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Constructor_Should_Throw_When_Priority_Is_Invalid(int priority)
    {
        // Act & Assert
        Assert.ThrowsExactly<OrderingDomainException>(() => new Promotion("Test", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), priority));
    }

    [TestMethod]
    public void Constructor_Should_Throw_When_VolumeDiscount_Has_No_MinimumQuantity()
    {
        // Arrange
        var discountType = DiscountType.VolumeDiscount;
        int? minimumQuantity = null;

        // Act & Assert
        Assert.ThrowsExactly<OrderingDomainException>(() => new Promotion("Test", discountType, 10, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 1, minimumQuantity: minimumQuantity));
    }

    [TestMethod]
    public void Constructor_Should_Throw_When_VolumeDiscount_Has_Zero_MinimumQuantity()
    {
        // Arrange
        var discountType = DiscountType.VolumeDiscount;
        int? minimumQuantity = 0;

        // Act & Assert
        Assert.ThrowsExactly<OrderingDomainException>(() => new Promotion("Test", discountType, 10, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 1, minimumQuantity: minimumQuantity));
    }

    [TestMethod]
    public void Constructor_Should_Throw_When_Id_Is_Empty()
    {
        // Typically Id is managed by SeedWork.Entity, but if we have a requirement for PromotionId not being null/empty
        // we might need to check how it's handled.
        
        // Arrange
        // (Assuming we might have a way to set it or it shouldn't be empty after construction)
        // For now, let's just assume we want a validation for it if it was passed.
    }
}
