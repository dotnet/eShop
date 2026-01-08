using eShop.BddTests.Support;
using eShop.BddTests.Drivers;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace eShop.BddTests.Hooks;

[Binding]
public class TestHooks
{
    private static IServiceProvider? _serviceProvider;
    private static InMemoryDatabaseSeeder? _seeder;

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        // Initialize test service provider
        var services = new ServiceCollection();
        ConfigureTestServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Seed test data once for entire test run
        _seeder = _serviceProvider.GetRequiredService<InMemoryDatabaseSeeder>();
        await _seeder.SeedTestDataAsync();

        Console.WriteLine("🌱 Test data seeded for BDD test run");
    }

    [BeforeScenario]
    public async Task BeforeScenario(ScenarioContext scenarioContext)
    {
        // Set up scenario-specific context
        scenarioContext["StartTime"] = DateTime.UtcNow;
        scenarioContext["CorrelationId"] = Guid.NewGuid().ToString();
        scenarioContext["ScenarioId"] = Guid.NewGuid().ToString();

        // Log scenario start
        Console.WriteLine($"🎬 Starting scenario: {scenarioContext.ScenarioInfo.Title}");
        Console.WriteLine($"   Correlation ID: {scenarioContext["CorrelationId"]}");
    }

    [AfterScenario]
    public async Task AfterScenario(ScenarioContext scenarioContext)
    {
        var startTime = scenarioContext.Get<DateTime>("StartTime");
        var duration = DateTime.UtcNow - startTime;

        // Clean up only scenario-specific data
        var drivers = new[]
        {
            _serviceProvider?.GetService<CatalogTestDriver>(),
            _serviceProvider?.GetService<BasketTestDriver>()
        };

        foreach (var driver in drivers.Where(d => d != null))
        {
            await driver!.CleanupScenarioDataAsync();
        }

        // Log scenario completion
        var status = scenarioContext.TestError == null ? "✅ PASSED" : "❌ FAILED";
        Console.WriteLine($"🏁 Scenario completed: {status} (Duration: {duration:mm\\:ss\\.fff})");

        if (scenarioContext.TestError != null)
        {
            Console.WriteLine($"   Error: {scenarioContext.TestError.Message}");
        }
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        // Clean up all seeded data
        if (_seeder != null)
        {
            await _seeder.ClearAllSeededDataAsync();
            Console.WriteLine("🧹 Test data cleaned up after BDD test run");
        }

        // Dispose service provider
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [BeforeStep]
    public void BeforeStep(ScenarioContext scenarioContext)
    {
        var stepInfo = scenarioContext.StepContext.StepInfo;
        Console.WriteLine($"   🔸 {stepInfo.StepDefinitionType}: {stepInfo.Text}");
    }

    [AfterStep]
    public void AfterStep(ScenarioContext scenarioContext)
    {
        var stepInfo = scenarioContext.StepContext.StepInfo;
        
        if (scenarioContext.TestError != null)
        {
            Console.WriteLine($"   ❌ Step failed: {stepInfo.Text}");
            Console.WriteLine($"      Error: {scenarioContext.TestError.Message}");
        }
    }

    private static void ConfigureTestServices(IServiceCollection services)
    {
        // Register in-memory repositories
        services.AddSingleton<InMemoryCatalogRepository>();
        services.AddSingleton<InMemoryBasketRepository>();

        // Register seeder
        services.AddSingleton<InMemoryDatabaseSeeder>();

        // Register test configuration
        services.AddSingleton<TestConfiguration>();

        // Register test drivers
        services.AddScoped<CatalogTestDriver>();
        services.AddScoped<BasketTestDriver>();

        // Register HTTP client
        services.AddHttpClient();

        // Register logging (console for tests)
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }
}

public class InMemoryDatabaseSeeder
{
    private readonly InMemoryCatalogRepository _catalogRepository;
    private readonly InMemoryBasketRepository _basketRepository;

    public InMemoryDatabaseSeeder(
        InMemoryCatalogRepository catalogRepository,
        InMemoryBasketRepository basketRepository)
    {
        _catalogRepository = catalogRepository;
        _basketRepository = basketRepository;
    }

    public async Task SeedTestDataAsync()
    {
        await _catalogRepository.SeedTestDataAsync();
        await _basketRepository.SeedTestDataAsync();
    }

    public async Task ClearAllSeededDataAsync()
    {
        await _catalogRepository.ClearSeededDataAsync();
        await _basketRepository.ClearSeededDataAsync();
    }
}

public class InMemoryBasketRepository
{
    private readonly Dictionary<string, object> _baskets = new();
    private readonly List<string> _seededIds = new();

    public async Task SeedTestDataAsync()
    {
        // Seed test baskets if needed
        await Task.CompletedTask;
    }

    public async Task ClearSeededDataAsync()
    {
        foreach (var id in _seededIds.ToList())
        {
            if (_baskets.ContainsKey(id))
            {
                _baskets.Remove(id);
                _seededIds.Remove(id);
            }
        }
        await Task.CompletedTask;
    }

    public async Task ClearScenarioDataAsync()
    {
        // Only clear scenario-specific data, preserve seeded data
        await Task.CompletedTask;
    }
}

public class BasketTestDriver
{
    private readonly HttpClient _httpClient;
    private readonly TestConfiguration _config;

    public BasketTestDriver(HttpClient httpClient, TestConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task CleanupScenarioDataAsync()
    {
        // Cleanup scenario-specific basket data
        await Task.CompletedTask;
    }
}