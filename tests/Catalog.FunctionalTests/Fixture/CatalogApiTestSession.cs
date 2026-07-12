using System.Collections.Concurrent;
using System.Reflection;

using eShop.Testing.Common;

namespace eShop.Catalog.FunctionalTests;

public sealed class CatalogApiTestSession : IAsyncDisposable
{
    private readonly ConcurrentDictionary<CatalogFunctionalTestMode, Lazy<Task<CatalogApiFixture>>> _fixtures = new();

    public async Task<CatalogApiTestHost> CreateHostAsync(
        Type testClass,
        string testMethodName,
        DelegatingHandler? handler = null,
        BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
    {
        var mode = CatalogFunctionalTestModeAttribute.Resolve(testClass, testMethodName, bindingFlags);
        var effectiveMode = FunctionalTestModeReader.ReadOverrideFromEnvironment() ?? mode;
        var fixture = await GetOrCreateFixtureAsync(effectiveMode);
        ArgumentNullException.ThrowIfNull(handler);
        var client = fixture.CreateLoggedClient(handler);

        return new CatalogApiTestHost(fixture, client);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var fixtureTask in _fixtures.Values)
        {
            if (!fixtureTask.IsValueCreated)
            {
                continue;
            }

            var fixture = await fixtureTask.Value;
            await fixture.DisposeAsync();
        }
    }

    private async Task<CatalogApiFixture> GetOrCreateFixtureAsync(CatalogFunctionalTestMode mode)
    {
        var lazy = _fixtures.GetOrAdd(
            mode,
            static m => new Lazy<Task<CatalogApiFixture>>(() => CreateAndInitializeFixtureAsync(m)));

        return await lazy.Value;
    }

    private static async Task<CatalogApiFixture> CreateAndInitializeFixtureAsync(CatalogFunctionalTestMode mode)
    {
        var fixture = new CatalogApiFixture(mode);
        await fixture.InitializeAsync();
        return fixture;
    }
}
