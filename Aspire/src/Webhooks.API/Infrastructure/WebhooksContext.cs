namespace Webhooks.API.Infrastructure;

/// <remarks>
/// Add migrations using the following command inside the 'Webhooks.API' project directory:
///
/// dotnet ef migrations add [migration-name]
/// </remarks>
public class WebhooksContext(DbContextOptions<WebhooksContext> options) : DbContext(options)
{
    public DbSet<WebhookSubscription> Subscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WebhookSubscription>(eb =>
        {
            eb.HasIndex(s => s.UserId);
            eb.HasIndex(s => s.Type);
        });
    }
}
