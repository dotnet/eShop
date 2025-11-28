using DataArc.Abstractions;

using EShop.Persistence.Models.Products;
using Microsoft.EntityFrameworkCore;

namespace EShop.Persistence.Context.Products
{
    public class ProductsContext : DbContext, IPipelineContext<ProductsContext>
    {
        public ProductsContext(DbContextOptions<ProductsContext> options) 
            : base(options) 
        { }

        public DbSet<ProductType> ProductType { get; set; }
        public DbSet<Product> Product { get; set; }

        override protected void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Product>()
              .HasOne(c => c.ProductType)
              .WithMany(t => t.Products)
              .HasForeignKey(c => c.CatalogTypeId);

            base.OnModelCreating(builder);
        }
    }
}