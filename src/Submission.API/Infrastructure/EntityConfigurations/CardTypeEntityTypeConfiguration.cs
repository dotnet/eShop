namespace Inked.Submission.API.Infrastructure.EntityConfigurations;

public class CardTypeEntityTypeConfiguration : IEntityTypeConfiguration<CardType>
{
    public void Configure(EntityTypeBuilder<CardType> builder)
    {
        builder
            .HasIndex(s => s.Type).IsUnique();
    }
    
}
