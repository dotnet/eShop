using System.Diagnostics.CodeAnalysis;

using eShop.Catalog.API.Infrastructure;
using eShop.Catalog.API.Model;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace eShop.Catalog.FunctionalTests.Configuration;

internal sealed class InMemoryCatalogContext : CatalogContext
{
    [SetsRequiredMembers]
    public InMemoryCatalogContext(DbContextOptions<InMemoryCatalogContext> options, IConfiguration configuration)
        : base(options, configuration)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<CatalogItem>().Ignore(item => item.Embedding);
    }
}
