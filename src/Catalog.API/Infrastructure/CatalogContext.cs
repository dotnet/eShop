using Catalog.API.Model;

namespace eShop.Catalog.API.Infrastructure;

/// <remarks>
/// Add migrations using the following command inside the 'Catalog.API' project directory:
///
/// dotnet ef migrations add --context CatalogContext [migration-name]
/// </remarks>
public class CatalogContext : DbContext
{
    public CatalogContext(DbContextOptions<CatalogContext> options, IConfiguration configuration) : base(options)
    {
    }

    public DbSet<CatalogItem> CatalogItems { get; set; }
    //public DbSet<CatalogBrand> CatalogBrands { get; set; }
    //public DbSet<CatalogType> CatalogTypes { get; set; }

    public DbSet<CatalogItemVariant> CatalogItemVariants { get; set; }

    public DbSet<EnchancedImages> EnhancedImages { get; set; }

    public DbSet<OriginalImages> OriginalImages { get; set; }

    public DbSet<CatalogFeature> CatalogFeatures { get; set; }

    public DbSet<CatalogKit> CatalogKits { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasPostgresExtension("vector");
        //builder.ApplyConfiguration(new CatalogBrandEntityTypeConfiguration());
        //builder.ApplyConfiguration(new CatalogTypeEntityTypeConfiguration());
        builder.ApplyConfiguration(new CatalogItemEntityTypeConfiguration());

        // Add the outbox table to this context
        builder.UseIntegrationEventLogs();
    }
}
