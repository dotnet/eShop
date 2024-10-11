namespace eShop.Catalog.API.Infrastructure.EntityConfigurations;

class CatalogItemEntityTypeConfiguration
    : IEntityTypeConfiguration<CatalogItem>
{
    public void Configure(EntityTypeBuilder<CatalogItem> builder)
    {
        builder.ToTable("Catalog");

        builder.Property(ci => ci.Name)
            .HasMaxLength(50);

        builder.Property(ci => ci.Embedding)
            .HasColumnType("vector(384)");

        builder.Property(ci => ci.SalePrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(ci => ci.IsOnSale)
            .HasDefaultValue(false);

        builder.Property(ci => ci.DiscountPercentage)
            .HasColumnType("decimal(5,2)");

        builder.Property(ci => ci.Geography)
            .HasMaxLength(100);

        builder.HasOne(ci => ci.CatalogBrand)
            .WithMany();

        builder.HasOne(ci => ci.CatalogType)
            .WithMany();

        builder.HasIndex(ci => ci.Name);
    }
}
