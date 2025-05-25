using Inked.Submission.API.Infrastructure.EntityConfigurations;

namespace Inked.Submission.API.Infrastructure;

/// <remarks>
///     Add migrations using the following command inside the 'Catalog.API' project directory:
///     dotnet ef migrations add --context CatalogContext [migration-name]
/// </remarks>
public class SubmissionContext : DbContext
{
    public SubmissionContext(DbContextOptions<SubmissionContext> options, IConfiguration configuration) : base(options)
    {
    }

    public DbSet<Model.Submission> Submissions { get; set; }
    public DbSet<CardTheme> CardThemes { get; set; }
    public DbSet<CardType> CardTypes { get; set; }

    public DbSet<MintingType> MintingTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasPostgresExtension("vector");
        builder.ApplyConfiguration(new SubmissionEntityTypeConfiguration());
        // Add the outbox table to this context
        builder.UseIntegrationEventLogs();
    }
}
