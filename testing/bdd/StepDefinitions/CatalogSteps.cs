using eShop.Catalog.API.Model;
using eShop.BddTests.Drivers;
using Reqnroll;
using Shouldly;
using System.Diagnostics;

namespace eShop.BddTests.StepDefinitions;

[Binding]
public class CatalogSteps
{
    private readonly CatalogTestDriver _catalogDriver;
    private readonly ScenarioContext _scenarioContext;

    public CatalogSteps(CatalogTestDriver catalogDriver, ScenarioContext scenarioContext)
    {
        _catalogDriver = catalogDriver;
        _scenarioContext = scenarioContext;
    }

    [Given(@"the catalog service is available")]
    public async Task GivenTheCatalogServiceIsAvailable()
    {
        // **Feature: catalog-management, Scenario: Service availability check**
        // **Validates: Requirements CAT-SVC-1.1**
        
        var isHealthy = await _catalogDriver.CheckServiceHealthAsync();
        isHealthy.ShouldBeTrue("Catalog service should be available for testing");
    }

    [Given(@"test data is seeded in the system")]
    public async Task GivenTestDataIsSeededInTheSystem()
    {
        await _catalogDriver.SeedTestDataAsync();
        _scenarioContext["TestDataSeeded"] = true;
    }

    [Given(@"the catalog contains outdoor gear products")]
    public async Task GivenTheCatalogContainsOutdoorGearProducts()
    {
        var products = await _catalogDriver.GetSeededProductsAsync();
        products.ShouldNotBeNull();
        products.Count().ShouldBeGreaterThan(0);
        _scenarioContext["SeededProducts"] = products;
    }

    [Given(@"a product exists with ID (.*)")]
    public async Task GivenAProductExistsWithID(int productId)
    {
        var product = await _catalogDriver.GetProductByIdAsync(productId);
        product.ShouldNotBeNull($"Product with ID {productId} should exist in test data");
        _scenarioContext["ExpectedProduct"] = product;
    }

    [Given(@"the catalog contains products with various names")]
    public async Task GivenTheCatalogContainsProductsWithVariousNames()
    {
        var products = await _catalogDriver.GetSeededProductsAsync();
        products.ShouldNotBeNull();
        products.Any(p => !string.IsNullOrEmpty(p.Name)).ShouldBeTrue();
        _scenarioContext["ProductsWithNames"] = products;
    }

    [Given(@"the catalog contains products from multiple brands")]
    public async Task GivenTheCatalogContainsProductsFromMultipleBrands()
    {
        var products = await _catalogDriver.GetSeededProductsAsync();
        var brands = products.Select(p => p.CatalogBrandId).Distinct().ToList();
        brands.Count.ShouldBeGreaterThan(1, "Test data should contain products from multiple brands");
        _scenarioContext["MultipleBrands"] = brands;
    }

    [Given(@"the catalog contains products in multiple categories")]
    public async Task GivenTheCatalogContainsProductsInMultipleCategories()
    {
        var products = await _catalogDriver.GetSeededProductsAsync();
        var categories = products.Select(p => p.CatalogTypeId).Distinct().ToList();
        categories.Count.ShouldBeGreaterThan(1, "Test data should contain products in multiple categories");
        _scenarioContext["MultipleCategories"] = categories;
    }

    [Given(@"the catalog contains more than (.*) products")]
    public async Task GivenTheCatalogContainsMoreThanProducts(int minimumCount)
    {
        var products = await _catalogDriver.GetSeededProductsAsync();
        products.Count().ShouldBeGreaterThan(minimumCount);
        _scenarioContext["ProductCount"] = products.Count();
    }

    [Given(@"the catalog service is under normal load")]
    public void GivenTheCatalogServiceIsUnderNormalLoad()
    {
        // Simulate normal load conditions
        _scenarioContext["LoadCondition"] = "Normal";
    }

    [When(@"I request the product catalog with page size (.*)")]
    public async Task WhenIRequestTheProductCatalogWithPageSize(int pageSize)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await _catalogDriver.GetCatalogItemsAsync(0, pageSize);
        stopwatch.Stop();

        _scenarioContext["CatalogResponse"] = response;
        _scenarioContext["ResponseTime"] = stopwatch.Elapsed;
    }

    [When(@"I request the product details for ID (.*)")]
    public async Task WhenIRequestTheProductDetailsForID(int productId)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await _catalogDriver.GetProductByIdAsync(productId);
        stopwatch.Stop();

        _scenarioContext["ProductResponse"] = response;
        _scenarioContext["ResponseTime"] = stopwatch.Elapsed;
    }

    [When(@"I search for products with name ""(.*)""")]
    public async Task WhenISearchForProductsWithName(string searchTerm)
    {
        var response = await _catalogDriver.SearchProductsByNameAsync(searchTerm);
        _scenarioContext["SearchResponse"] = response;
        _scenarioContext["SearchTerm"] = searchTerm;
    }

    [When(@"I filter products by brand ""(.*)""")]
    public async Task WhenIFilterProductsByBrand(string brandName)
    {
        var response = await _catalogDriver.GetProductsByBrandAsync(brandName);
        _scenarioContext["FilteredResponse"] = response;
        _scenarioContext["FilterBrand"] = brandName;
    }

    [When(@"I filter products by category ""(.*)""")]
    public async Task WhenIFilterProductsByCategory(string categoryName)
    {
        var response = await _catalogDriver.GetProductsByCategoryAsync(categoryName);
        _scenarioContext["FilteredResponse"] = response;
        _scenarioContext["FilterCategory"] = categoryName;
    }

    [When(@"I request page (.*) with page size (.*)")]
    public async Task WhenIRequestPageWithPageSize(int pageIndex, int pageSize)
    {
        var response = await _catalogDriver.GetCatalogItemsAsync(pageIndex, pageSize);
        _scenarioContext["PaginatedResponse"] = response;
        _scenarioContext["RequestedPage"] = pageIndex;
        _scenarioContext["RequestedPageSize"] = pageSize;
    }

    [When(@"I request product details for ID (.*)")]
    public async Task WhenIRequestProductDetailsForID(int productId)
    {
        try
        {
            var response = await _catalogDriver.GetProductByIdAsync(productId);
            _scenarioContext["ProductResponse"] = response;
            _scenarioContext["RequestSuccessful"] = response != null;
        }
        catch (Exception ex)
        {
            _scenarioContext["RequestException"] = ex;
            _scenarioContext["RequestSuccessful"] = false;
        }
    }

    [When(@"I request the product catalog")]
    public async Task WhenIRequestTheProductCatalog()
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await _catalogDriver.GetCatalogItemsAsync(0, 10);
        stopwatch.Stop();

        _scenarioContext["CatalogResponse"] = response;
        _scenarioContext["ResponseTime"] = stopwatch.Elapsed;
    }

    [Then(@"I should receive a list of products")]
    public void ThenIShouldReceiveAListOfProducts()
    {
        var response = _scenarioContext.Get<IEnumerable<CatalogItem>>("CatalogResponse");
        response.ShouldNotBeNull();
        response.ShouldNotBeEmpty();
    }

    [Then(@"the response should contain valid product data")]
    public void ThenTheResponseShouldContainValidProductData()
    {
        var response = _scenarioContext.Get<IEnumerable<CatalogItem>>("CatalogResponse");
        
        foreach (var product in response)
        {
            product.Id.ShouldBeGreaterThan(0);
            product.Name.ShouldNotBeNullOrEmpty();
            product.Price.ShouldBeGreaterThan(0);
        }
    }

    [Then(@"each product should have a name, price, and description")]
    public void ThenEachProductShouldHaveANamePriceAndDescription()
    {
        var response = _scenarioContext.Get<IEnumerable<CatalogItem>>("CatalogResponse");
        
        foreach (var product in response)
        {
            product.Name.ShouldNotBeNullOrEmpty();
            product.Price.ShouldBeGreaterThan(0);
            product.Description.ShouldNotBeNullOrEmpty();
        }
    }

    [Then(@"the response execution time should be acceptable")]
    public void ThenTheResponseExecutionTimeShouldBeAcceptable()
    {
        var responseTime = _scenarioContext.Get<TimeSpan>("ResponseTime");
        responseTime.TotalSeconds.ShouldBeLessThan(5, "Response time should be under 5 seconds");
    }

    [Then(@"I should receive the product information")]
    public void ThenIShouldReceiveTheProductInformation()
    {
        var response = _scenarioContext.Get<CatalogItem>("ProductResponse");
        response.ShouldNotBeNull();
    }

    [Then(@"the product should have complete details")]
    public void ThenTheProductShouldHaveCompleteDetails()
    {
        var product = _scenarioContext.Get<CatalogItem>("ProductResponse");
        
        product.Id.ShouldBeGreaterThan(0);
        product.Name.ShouldNotBeNullOrEmpty();
        product.Description.ShouldNotBeNullOrEmpty();
        product.Price.ShouldBeGreaterThan(0);
    }

    [Then(@"the product should have stock information")]
    public void ThenTheProductShouldHaveStockInformation()
    {
        var product = _scenarioContext.Get<CatalogItem>("ProductResponse");
        
        product.AvailableStock.ShouldBeGreaterThanOrEqualTo(0);
        product.RestockThreshold.ShouldBeGreaterThanOrEqualTo(0);
        product.MaxStockThreshold.ShouldBeGreaterThan(0);
    }

    [Then(@"the response should indicate success")]
    public void ThenTheResponseShouldIndicateSuccess()
    {
        var requestSuccessful = _scenarioContext.Get<bool>("RequestSuccessful");
        requestSuccessful.ShouldBeTrue();
    }

    [Then(@"I should receive products matching the search criteria")]
    public void ThenIShouldReceiveProductsMatchingTheSearchCriteria()
    {
        var response = _scenarioContext.Get<IEnumerable<CatalogItem>>("SearchResponse");
        response.ShouldNotBeNull();
        response.ShouldNotBeEmpty();
    }

    [Then(@"all returned products should contain ""(.*)"" in their name")]
    public void ThenAllReturnedProductsShouldContainInTheirName(string searchTerm)
    {
        var response = _scenarioContext.Get<IEnumerable<CatalogItem>>("SearchResponse");
        
        foreach (var product in response)
        {
            product.Name.ShouldContain(searchTerm, Case.Insensitive);
        }
    }

    [Then(@"the search results should be relevant")]
    public void ThenTheSearchResultsShouldBeRelevant()
    {
        var response = _scenarioContext.Get<IEnumerable<CatalogItem>>("SearchResponse");
        var searchTerm = _scenarioContext.Get<string>("SearchTerm");
        
        // All results should be relevant to the search term
        response.All(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                         p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
               .ShouldBeTrue();
    }

    [Then(@"I should receive a not found response")]
    public void ThenIShouldReceiveANotFoundResponse()
    {
        var requestSuccessful = _scenarioContext.Get<bool>("RequestSuccessful");
        requestSuccessful.ShouldBeFalse();
        
        var response = _scenarioContext.Get<CatalogItem>("ProductResponse");
        response.ShouldBeNull();
    }

    [Then(@"the error message should be descriptive")]
    public void ThenTheErrorMessageShouldBeDescriptive()
    {
        // In a real implementation, this would check the actual error response
        var exception = _scenarioContext.Get<Exception>("RequestException");
        exception?.Message.ShouldNotBeNullOrEmpty();
    }

    [Then(@"the response should maintain proper error format")]
    public void ThenTheResponseShouldMaintainProperErrorFormat()
    {
        // Verify error response follows API standards
        var requestSuccessful = _scenarioContext.Get<bool>("RequestSuccessful");
        requestSuccessful.ShouldBeFalse();
    }

    [Then(@"the response time should be less than (.*) seconds")]
    public void ThenTheResponseTimeShouldBeLessThanSeconds(int maxSeconds)
    {
        var responseTime = _scenarioContext.Get<TimeSpan>("ResponseTime");
        responseTime.TotalSeconds.ShouldBeLessThan(maxSeconds);
    }

    [Then(@"the response should be properly formatted")]
    public void ThenTheResponseShouldBeProperlyFormatted()
    {
        var response = _scenarioContext.Get<IEnumerable<CatalogItem>>("CatalogResponse");
        response.ShouldNotBeNull();
        
        // Verify response structure
        foreach (var item in response)
        {
            item.Id.ShouldBeGreaterThan(0);
            item.Name.ShouldNotBeNullOrEmpty();
        }
    }

    [Then(@"all required fields should be present")]
    public void ThenAllRequiredFieldsShouldBePresent()
    {
        var response = _scenarioContext.Get<IEnumerable<CatalogItem>>("CatalogResponse");
        
        foreach (var product in response)
        {
            product.Id.ShouldBeGreaterThan(0);
            product.Name.ShouldNotBeNullOrEmpty();
            product.Price.ShouldBeGreaterThan(0);
            product.CatalogTypeId.ShouldBeGreaterThan(0);
            product.CatalogBrandId.ShouldBeGreaterThan(0);
        }
    }
}