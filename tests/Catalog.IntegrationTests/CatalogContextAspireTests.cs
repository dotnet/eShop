using Aspire.Hosting;
using Aspire.Hosting.Testing;
using eShop.Catalog.API.Infrastructure;
using eShop.Catalog.API.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.IntegrationTests;

public class CatalogContextAspireTests : IAsyncLifetime
{
    private DistributedApplication _app = null!;
    private string _connectionString = null!;

    public async ValueTask InitializeAsync()
    {
        // 1. Configuramos el builder apuntando al AppHost de Aspire.
        // Esto lee toda la topología de recursos (Bases de datos, Redis, RabbitMQ)
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.eShop_AppHost>();

        _app = await appHost.BuildAsync();

        // 2. Iniciamos la orquestación. Aspire levanta los contenedores reales en Docker.
        await _app.StartAsync();

        // 3. Extraemos la cadena de conexión generada dinámicamente.
        // "catalogdb" es el nombre del recurso definido en tu eShop.AppHost
        _connectionString = await _app.GetConnectionStringAsync("catalogdb")
            ?? throw new InvalidOperationException("No se pudo obtener la connection string del recurso catalogdb");
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await _app.StopAsync();
        if (_app is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _app.Dispose();
        }
    }

    private CatalogContext CreateContext()
    {
        var services = new ServiceCollection();

        services.AddDbContext<CatalogContext>(options =>
            options.UseNpgsql(_connectionString, npgsqlOptions =>
            {
                npgsqlOptions.UseVector();
            }));

        var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<CatalogContext>();
    }

    [Fact]
    public async Task Can_Add_And_Retrieve_CatalogItem_Using_Aspire_Db()
    {
        // --- ARRANGE ---
        await using var arrangeContext = CreateContext();
        // 1. En lugar de crear marcas nuevas, tomamos la primera que el Seeder 
        // de eShop ya insertó en nuestra base de datos.
        var existingBrand = await arrangeContext.CatalogBrands.FirstAsync(CancellationToken.None);
        var existingType = await arrangeContext.CatalogTypes.FirstAsync(CancellationToken.None);

        var biggerId = await arrangeContext.CatalogItems.MaxAsync(i => (int?)i.Id, CancellationToken.None) ?? 0;
        // 2. Generamos un nombre único para que el test sea repetible sin limpiar la DB
        string uniqueProductName = $"Aspire {Guid.NewGuid()}";

        var newItem = new CatalogItem(uniqueProductName)
        {
            Id = biggerId + 1,
            Description = "Testing EF Core with Aspire Hosting",
            Price = 25.50m,
            CatalogBrandId = existingBrand.Id,
            CatalogTypeId = existingType.Id,
            AvailableStock = 50
        };

        // --- ACT ---
        arrangeContext.CatalogItems.Add(newItem);
        await arrangeContext.SaveChangesAsync(CancellationToken.None);

        // --- ASSERT ---
        // Usamos una nueva instancia para evitar falsos positivos por el caché de EF
        await using var assertContext = CreateContext();

        var savedItem = await assertContext.CatalogItems
            .Include(i => i.CatalogBrand)
            .Include(i => i.CatalogType)
            .FirstOrDefaultAsync(i => i.Name == uniqueProductName, CancellationToken.None);

        Assert.NotNull(savedItem);
        Assert.Equal(uniqueProductName, savedItem.Name);
        Assert.Equal(25.50m, savedItem.Price);
    }


}
