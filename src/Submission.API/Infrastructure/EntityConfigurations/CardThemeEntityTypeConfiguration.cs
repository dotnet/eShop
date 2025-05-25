namespace Inked.Submission.API.Infrastructure.EntityConfigurations;

public class CardThemeEntityTypeConfiguration : IEntityTypeConfiguration<CardTheme>
{
    public void Configure(EntityTypeBuilder<CardTheme> builder)
    {
        builder
            .HasIndex(s => s.Theme).IsUnique();
    }
    
}
