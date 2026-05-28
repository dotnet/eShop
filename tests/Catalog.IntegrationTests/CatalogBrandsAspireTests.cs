using Aspire.Hosting;
using Aspire.Hosting.Testing;
using eShop.Catalog.API.Infrastructure;
using eShop.Catalog.API.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.IntegrationTests;

public class CatalogBrandsAspireTests : IAsyncLifetime
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
    public async Task Can_Add_And_Retrieve_CatalogBrand_Using_Aspire_Db()
    {
        // --- ARRANGE ---
        await using var arrangeContext = CreateContext();
        
        // Generamos un nombre único para que el test sea repetible sin colisiones en la DB
        string uniqueBrandName = $"Brand {Guid.NewGuid()}";

        var newBrand = new CatalogBrand(uniqueBrandName);

        // --- ACT ---
        arrangeContext.CatalogBrands.Add(newBrand);
        await arrangeContext.SaveChangesAsync(CancellationToken.None);

        // --- ASSERT ---
        // Usamos una nueva instancia para evitar falsos positivos por el caché de EF Core
        await using var assertContext = CreateContext();

        var savedBrand = await assertContext.CatalogBrands
            .FirstOrDefaultAsync(b => b.Brand == uniqueBrandName, CancellationToken.None);

        Assert.NotNull(savedBrand);
        Assert.Equal(uniqueBrandName, savedBrand.Brand);
        Assert.True(savedBrand.Id > 0);
    }
}
