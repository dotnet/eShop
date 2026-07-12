using eShop.Catalog.API.Infrastructure;
using eShop.Catalog.API.Model;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Pgvector;

namespace eShop.Catalog.FunctionalTests.Infrastructure;

internal static class CatalogDatabaseHelper
{
    public static async Task EnsureInMemorySeededAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        await SeedCatalogContextAsync(context);
    }

    public static async Task EnsurePostgresSeededAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        await context.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<IDbSeeder<CatalogContext>>();
        await seeder.SeedAsync(context);
    }

    public static async Task<CatalogItem?> LoadItemFromScopedContextAsync(IServiceProvider services, int id)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        return await context.CatalogItems.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
    }

    public static async Task<long> GetCatalogItemCountFromScopedContextAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        return await context.CatalogItems.LongCountAsync();
    }

    public static async Task<long> GetCatalogItemCountFromPostgresAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<CatalogContext>()
            .UseNpgsql(connectionString, builder => builder.UseVector())
            .Options;

        using var context = new CatalogContext(options, new ConfigurationBuilder().Build());
        return await context.CatalogItems.LongCountAsync();
    }

    public static async Task<CatalogItem?> LoadItemFromPostgresAsync(string connectionString, int id)
    {
        var options = new DbContextOptionsBuilder<CatalogContext>()
            .UseNpgsql(connectionString, builder => builder.UseVector())
            .Options;

        using var context = new CatalogContext(options, new ConfigurationBuilder().Build());
        return await context.CatalogItems.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
    }

    private static async Task SeedCatalogContextAsync(CatalogContext context)
    {
        if (await context.CatalogItems.AnyAsync())
        {
            return;
        }

        var sourceItems = await CatalogSourceData.LoadSourceEntriesAsync();

        var brands = sourceItems
            .Select(x => x.Brand)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .Select(x => new CatalogBrand(x!))
            .ToList();

        var types = sourceItems
            .Select(x => x.Type)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .Select(x => new CatalogType(x!))
            .ToList();

        context.CatalogBrands.AddRange(brands);
        context.CatalogTypes.AddRange(types);
        await context.SaveChangesAsync();

        var brandsByName = await context.CatalogBrands.ToDictionaryAsync(x => x.Brand, x => x.Id);
        var typesByName = await context.CatalogTypes.ToDictionaryAsync(x => x.Type, x => x.Id);

        context.CatalogItems.AddRange(sourceItems
            .Where(source => source.Name is not null && source.Brand is not null && source.Type is not null)
            .Select(source => CatalogSourceData.CreateCatalogItem(source, brandsByName[source.Brand!], typesByName[source.Type!])));

        await context.SaveChangesAsync();
    }
}
