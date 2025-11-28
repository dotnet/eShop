using DataArc.Abstractions;

using EShop.Persistence.Models.Catalog;
using Microsoft.EntityFrameworkCore;

namespace EShop.Persistence.Contexts.Cataloging
{
    public class CatalogContext : DbContext, IPipelineContext<CatalogContext>
    {
        public CatalogContext(DbContextOptions<CatalogContext> options) : base(options)
        {
        }

        public DbSet<Catalog> Catalogs { get; set; }
        public DbSet<CatalogItem> CatalogItems { get; set; }
        public DbSet<CatalogBrand> CatalogBrands { get; set; }
        public DbSet<CatalogType> CatalogTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Catalog>()
                .HasOne(c => c.CatalogType)
                .WithMany(t => t.Catalogs)
                .HasForeignKey(c => c.CatalogTypeId);

            base.OnModelCreating(builder);
        }
    }
}