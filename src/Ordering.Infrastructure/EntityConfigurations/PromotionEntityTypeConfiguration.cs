using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Ordering.Infrastructure.EntityConfigurations;

class PromotionEntityTypeConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> promotionConfiguration)
    {
        promotionConfiguration.ToTable("promotions");

        promotionConfiguration.Ignore(b => b.DomainEvents);

        promotionConfiguration.Property(o => o.Id)
            .UseHiLo("promotionseq");

        promotionConfiguration.Property(o => o.Name)
            .HasMaxLength(200)
            .IsRequired();

        promotionConfiguration.Property(o => o.DiscountType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Configure private fields for collections if necessary
        // promotionConfiguration.Metadata.FindNavigation(nameof(Promotion.ApplicableCategories))
        //    .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
