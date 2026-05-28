using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using eShop.Catalog.API.Infrastructure;
using eShop.Catalog.API.Model;
using Xunit;

namespace eShop.Catalog.FunctionalTests;

public class TestCatalogContext : CatalogContext
{
    [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
    public TestCatalogContext(DbContextOptions<CatalogContext> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<CatalogItem>().Ignore(c => c.Embedding); 
    }
}

public class CatalogApiFunctionalTests
{
    private CatalogContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CatalogContext>()
            .UseInMemoryDatabase($"InMemoryCatalog_{Guid.NewGuid()}")
            .Options;
        
        var context = new TestCatalogContext(options);
        
        var type = new CatalogType("Test Type") { Id = 1 };
        context.CatalogTypes.Add(type);
        
        var item = new CatalogItem("Test Product") { Id = 1, Price = 10, CatalogTypeId = 1 };
        context.CatalogItems.Add(item);
        
        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task AddItem_SavesToDatabase_Successfully()
    {
        var context = CreateContext();
        var newItem = new CatalogItem("New Product") { Id = 2, Price = 50, CatalogTypeId = 1 };
        
        context.CatalogItems.Add(newItem);
        await context.SaveChangesAsync();
        
        var saved = await context.CatalogItems.FirstOrDefaultAsync(x => x.Id == 2);
        Assert.NotNull(saved);
        Assert.Equal("New Product", saved.Name);
    }

    [Fact]
    public async Task GetItem_WhenExists_ReturnsCorrectItem()
    {
        var context = CreateContext();
        
        var item = await context.CatalogItems.FirstOrDefaultAsync(x => x.Id == 1);
        
        Assert.NotNull(item);
        Assert.Equal(10, item.Price);
    }

    [Fact]
    public async Task GetItem_WhenDoesNotExist_ReturnsNull()
    {
        var context = CreateContext();
        
        var item = await context.CatalogItems.FirstOrDefaultAsync(x => x.Id == 999);
        
        Assert.Null(item);
    }

    [Fact]
    public async Task GetAllCatalogTypes_ReturnsConfiguredTypes()
    {
        var context = CreateContext();
        
        var types = await context.CatalogTypes.ToListAsync();
        
        Assert.NotEmpty(types);
        Assert.Single(types);
        Assert.Equal("Test Type", types[0].Type);
    }
}