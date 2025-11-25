using Microsoft.EntityFrameworkCore;
using DataArc.Abstractions;

using EShop.Persistence.Models.Catalog;

namespace EShop.Persistence.Contexts.Cataloging
{
    public class CatalogContext : DbContext, IPipelineContext<CatalogContext>
    {
        public CatalogContext(DbContextOptions<CatalogContext> options) : base(options)
        {
        }

        public DbSet<CatalogItem> CatalogItems { get; set; }
        public DbSet<CatalogBrand> CatalogBrands { get; set; }
        public DbSet<CatalogType> CatalogTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            
        }
    }
}