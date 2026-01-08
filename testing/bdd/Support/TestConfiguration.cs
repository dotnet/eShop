namespace eShop.BddTests.Support;

public class TestConfiguration
{
    public string BaseUrl { get; set; } = "https://localhost:5001";
    public int TimeoutMs { get; set; } = 30000;
    public string Environment { get; set; } = "test";
    public bool UseInMemoryRepositories { get; set; } = true;
    public string DatabaseConnectionString { get; set; } = "Server=localhost;Database=eShopTest;Trusted_Connection=true;";
}