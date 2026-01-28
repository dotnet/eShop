namespace eShop.Ordering.UnitTests.Domain;

using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;

[TestClass]
public class PromotionalDiscountTests
{
    [TestMethod]
    public void DiscountType_values_are_correct()
    {
        // Assert
        Assert.AreEqual(1, (int)DiscountType.PercentageDiscount);
        Assert.AreEqual(2, (int)DiscountType.FixedAmountDiscount);
        Assert.AreEqual(3, (int)DiscountType.VolumeDiscount);
        Assert.AreEqual(4, (int)DiscountType.CategoryDiscount);
        Assert.AreEqual(5, (int)DiscountType.FirstTimeCustomerDiscount);
    }

    [TestMethod]
    public void AppliedDiscount_creation_success()
    {
        // Arrange
        var promotionId = Guid.NewGuid().ToString();
        var promotionName = "Test Promo";
        var discountAmount = 10.50m;
        var appliedAt = DateTime.UtcNow;
        var itemCount = 2;

        // Act
        var appliedDiscount = new AppliedDiscount(promotionId, promotionName, discountAmount, appliedAt, itemCount);

        // Assert
        Assert.AreEqual(promotionId, appliedDiscount.PromotionId);
        Assert.AreEqual(promotionName, appliedDiscount.PromotionName);
        Assert.AreEqual(discountAmount, appliedDiscount.DiscountAmount);
        Assert.AreEqual(appliedAt, appliedDiscount.AppliedAt);
        Assert.AreEqual(itemCount, appliedDiscount.ItemCount);
    }

    [TestMethod]
    public void DiscountCalculationResult_calculation_is_correct()
    {
        // Arrange
        var originalAmount = 100m;
        var appliedDiscounts = new List<AppliedDiscount>
        {
            new AppliedDiscount(Guid.NewGuid().ToString(), "Promo 1", 10m, DateTime.UtcNow, 1),
            new AppliedDiscount(Guid.NewGuid().ToString(), "Promo 2", 5.5m, DateTime.UtcNow, 1)
        };
        var skippedPromotions = new List<string> { "Expired Promo" };

        // Act
        var result = new DiscountCalculationResult(originalAmount, appliedDiscounts, skippedPromotions);

        // Assert
        Assert.AreEqual(100m, result.OriginalAmount);
        Assert.AreEqual(15.5m, result.TotalDiscount);
        Assert.AreEqual(84.5m, result.FinalAmount);
        Assert.AreEqual(2, result.AppliedDiscounts.Count);
        Assert.AreEqual(1, result.SkippedPromotions.Count);
    }

    [TestMethod]
    public void AppliedDiscount_equality_works()
    {
        // Arrange
        var promoId = Guid.NewGuid().ToString();
        var appliedAt = DateTime.UtcNow;
        var d1 = new AppliedDiscount(promoId, "Promo", 10m, appliedAt, 1);
        var d2 = new AppliedDiscount(promoId, "Promo", 10m, appliedAt, 1);

        // Assert
        Assert.AreEqual(d1, d2);
    }
}
