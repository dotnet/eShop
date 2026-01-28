using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Infrastructure;
using eShop.Ordering.Infrastructure.Repositories;

namespace eShop.Ordering.UnitTests.Infrastructure;

[TestClass]
public class PromotionRepositoryTests
{
    [TestMethod]
    public async Task Add_promotion_should_persist()
    {
        var dbOptions = new DbContextOptionsBuilder<OrderingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        using var context = new OrderingContext(dbOptions);
        var repository = new PromotionRepository(context);
        var promotion = new Promotion("Test Promo", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);

        repository.Add(promotion);
        await context.SaveChangesAsync();

        var result = await context.Promotions.FirstOrDefaultAsync(p => p.Name == "Test Promo");
        Assert.IsNotNull(result);
        Assert.AreEqual(10, result.DiscountValue);
    }

    [TestMethod]
    public async Task GetByIdAsync_should_return_correct_promotion()
    {
        var dbOptions = new DbContextOptionsBuilder<OrderingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        using var context = new OrderingContext(dbOptions);
        var promotion = new Promotion("GetById Test", DiscountType.PercentageDiscount, 15, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        context.Promotions.Add(promotion);
        await context.SaveChangesAsync();

        var repository = new PromotionRepository(context);
        var result = await repository.GetByIdAsync(promotion.Id);

        Assert.IsNotNull(result);
        Assert.AreEqual("GetById Test", result.Name);
    }

    [TestMethod]
    public async Task GetActivePromotionsAsync_should_return_only_active_promotions()
    {
        var dbOptions = new DbContextOptionsBuilder<OrderingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        using var context = new OrderingContext(dbOptions);

        var active1 = new Promotion("Active 1", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var active2 = new Promotion("Active 2", DiscountType.PercentageDiscount, 20, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(2), 2);
        var inactive = new Promotion("Inactive", DiscountType.PercentageDiscount, 30, DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(-1), 3);
        
        context.Promotions.AddRange(active1, active2, inactive);
        await context.SaveChangesAsync();

        var repository = new PromotionRepository(context);
        var results = await repository.GetActivePromotionsAsync();

        Assert.AreEqual(2, results.Count());
        Assert.IsTrue(results.Any(p => p.Name == "Active 1"));
        Assert.IsTrue(results.Any(p => p.Name == "Active 2"));
        Assert.IsFalse(results.Any(p => p.Name == "Inactive"));
    }

    [TestMethod]
    public async Task Update_promotion_should_modify_existing_record()
    {
        var dbOptions = new DbContextOptionsBuilder<OrderingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        using var context = new OrderingContext(dbOptions);
        var promotion = new Promotion("Update Test", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        context.Promotions.Add(promotion);
        await context.SaveChangesAsync();

        var repository = new PromotionRepository(context);
        promotion.Deactivate();
        repository.Update(promotion);
        await context.SaveChangesAsync();

        var result = await context.Promotions.FindAsync(promotion.Id);
        Assert.IsFalse(result.IsActive);
    }

    [TestMethod]
    public async Task Delete_promotion_should_remove_from_database()
    {
        var dbOptions = new DbContextOptionsBuilder<OrderingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        using var context = new OrderingContext(dbOptions);
        var promotion = new Promotion("Delete Test", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        context.Promotions.Add(promotion);
        await context.SaveChangesAsync();

        var repository = new PromotionRepository(context);
        repository.Delete(promotion);
        await context.SaveChangesAsync();

        var result = await context.Promotions.FindAsync(promotion.Id);
        Assert.IsNull(result);
    }
}
