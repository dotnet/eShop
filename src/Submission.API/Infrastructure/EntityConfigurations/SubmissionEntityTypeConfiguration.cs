namespace Inked.Submission.API.Infrastructure.EntityConfigurations;

public class SubmissionEntityTypeConfiguration : IEntityTypeConfiguration<Model.Submission>
{
    public void Configure(EntityTypeBuilder<Model.Submission> builder)
    {
        builder.Property(ci => ci.Embedding)
            .HasColumnType("vector(384)");

        builder
            .HasOne(s => s.CardType)
            .WithMany()
            .HasForeignKey(s => s.CardTypeId);

        builder
            .HasIndex(s => s.Title).IsUnique();

        builder
            .HasOne(s => s.MintingType)
            .WithMany()
            .HasForeignKey(s => s.MintingTypeId);

        builder
            .HasMany(s => s.CardThemes)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "SubmissionCardTheme",
                j => j.HasOne<CardTheme>().WithMany().HasForeignKey("CardThemeId"),
                j => j.HasOne<Model.Submission>().WithMany().HasForeignKey("SubmissionId"));
    }
}
