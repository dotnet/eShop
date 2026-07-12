using System.Collections.Concurrent;
using System.Reflection;

using Asp.Versioning;
using Asp.Versioning.Http;

using eShop.Testing.Common;

namespace eShop.Ordering.FunctionalTests;

public sealed class OrderingApiTestSession : IAsyncDisposable
{
    private readonly ConcurrentDictionary<OrderingFunctionalTestMode, Lazy<Task<OrderingApiFixture>>> _fixtures = new();

    public async Task<OrderingApiTestHost> CreateHostAsync(
        Type testClass,
        string testMethodName,
        DelegatingHandler? handler = null,
        BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
    {
        var mode = OrderingFunctionalTestModeAttribute.Resolve(testClass, testMethodName, bindingFlags);
        var effectiveMode = FunctionalTestModeReader.ReadOverrideFromEnvironment() ?? mode;
        var fixture = await GetOrCreateFixtureAsync(effectiveMode);

        handler ??= new ApiVersionHandler(new QueryStringApiVersionWriter(), new ApiVersion(1.0));
        var client = fixture.CreateLoggedClient(handler);

        return new OrderingApiTestHost(fixture, client);
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

    private async Task<OrderingApiFixture> GetOrCreateFixtureAsync(OrderingFunctionalTestMode mode)
    {
        var lazy = _fixtures.GetOrAdd(
            mode,
            static m => new Lazy<Task<OrderingApiFixture>>(() => CreateAndInitializeFixtureAsync(m)));

        return await lazy.Value;
    }

    private static async Task<OrderingApiFixture> CreateAndInitializeFixtureAsync(OrderingFunctionalTestMode mode)
    {
        var fixture = new OrderingApiFixture(mode);
        await fixture.InitializeAsync();
        return fixture;
    }
}
