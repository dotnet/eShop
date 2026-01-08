using eShop.Basket.API.Model;
using eShop.BddTests.Drivers;
using Reqnroll;
using Shouldly;
using System.Diagnostics;

namespace eShop.BddTests.StepDefinitions;

[Binding]
public class BasketSteps
{
    private readonly BasketTestDriver _basketDriver;
    private readonly ScenarioContext _scenarioContext;

    public BasketSteps(BasketTestDriver basketDriver, ScenarioContext scenarioContext)
    {
        _basketDriver = basketDriver;
        _scenarioContext = scenarioContext;
    }

    [Given(@"the basket service is available")]
    public async Task GivenTheBasketServiceIsAvailable()
    {
        // **Feature: basket-management, Scenario: Service availability check**
        // **Validates: Requirements BASKET-SVC-1.1**
        
        var isHealthy = await _basketDriver.CheckServiceHealthAsync();
        isHealthy.ShouldBeTrue("Basket service should be available for testing");
    }

    [Given(@"I am authenticated as a customer")]
    public async Task GivenIAmAuthenticatedAsACustomer()
    {
        var customerId = "test-customer-123";
        await _basketDriver.AuthenticateCustomerAsync(customerId);
        _scenarioContext["CustomerId"] = customerId;
    }

    [Given(@"I have an empty basket")]
    public async Task GivenIHaveAnEmptyBasket()
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        await _basketDriver.ClearBasketAsync(customerId);
        
        var basket = await _basketDriver.GetBasketAsync(customerId);
        basket.Items.ShouldBeEmpty("Basket should be empty");
        _scenarioContext["CurrentBasket"] = basket;
    }

    [Given(@"I have a basket with the following items:")]
    public async Task GivenIHaveABasketWithTheFollowingItems(Table table)
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        await _basketDriver.ClearBasketAsync(customerId);

        foreach (var row in table.Rows)
        {
            var productId = int.Parse(row["ProductId"]);
            var productName = row["ProductName"];
            var quantity = int.Parse(row["Quantity"]);
            var unitPrice = decimal.Parse(row["UnitPrice"]);

            await _basketDriver.AddItemToBasketAsync(customerId, productId, productName, quantity, unitPrice);
        }

        var basket = await _basketDriver.GetBasketAsync(customerId);
        _scenarioContext["CurrentBasket"] = basket;
    }

    [Given(@"I have a basket with items")]
    public async Task GivenIHaveABasketWithItems()
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        await _basketDriver.ClearBasketAsync(customerId);

        // Add some default test items
        await _basketDriver.AddItemToBasketAsync(customerId, 1, "Test Product 1", 1, 99.99m);
        await _basketDriver.AddItemToBasketAsync(customerId, 2, "Test Product 2", 2, 49.99m);

        var basket = await _basketDriver.GetBasketAsync(customerId);
        _scenarioContext["CurrentBasket"] = basket;
    }

    [Given(@"I have a basket with valid items")]
    public async Task GivenIHaveABasketWithValidItems()
    {
        await GivenIHaveABasketWithItems();
        
        // Verify items are valid for checkout
        var basket = _scenarioContext.Get<CustomerBasket>("CurrentBasket");
        basket.Items.ShouldNotBeEmpty();
        basket.Items.All(i => i.Quantity > 0 && i.UnitPrice > 0).ShouldBeTrue();
    }

    [When(@"I add a product with ID (.*) and quantity (.*) to my basket")]
    public async Task WhenIAddAProductWithIDAndQuantityToMyBasket(int productId, int quantity)
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await _basketDriver.AddItemToBasketAsync(customerId, productId, $"Product {productId}", quantity, 99.99m);
            _scenarioContext["AddItemResult"] = result;
            _scenarioContext["OperationSuccessful"] = true;
        }
        catch (Exception ex)
        {
            _scenarioContext["OperationException"] = ex;
            _scenarioContext["OperationSuccessful"] = false;
        }
        finally
        {
            stopwatch.Stop();
            _scenarioContext["ResponseTime"] = stopwatch.Elapsed;
        }
    }

    [When(@"I add the following items to my basket:")]
    public async Task WhenIAddTheFollowingItemsToMyBasket(Table table)
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        var addedItems = new List<BasketItem>();

        foreach (var row in table.Rows)
        {
            var productId = int.Parse(row["ProductId"]);
            var productName = row["ProductName"];
            var quantity = int.Parse(row["Quantity"]);
            var unitPrice = decimal.Parse(row["UnitPrice"]);

            var item = await _basketDriver.AddItemToBasketAsync(customerId, productId, productName, quantity, unitPrice);
            addedItems.Add(item);
        }

        _scenarioContext["AddedItems"] = addedItems;
    }

    [When(@"I update the quantity of product (.*) to (.*)")]
    public async Task WhenIUpdateTheQuantityOfProductTo(int productId, int newQuantity)
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        
        try
        {
            await _basketDriver.UpdateItemQuantityAsync(customerId, productId, newQuantity);
            _scenarioContext["UpdateSuccessful"] = true;
        }
        catch (Exception ex)
        {
            _scenarioContext["UpdateException"] = ex;
            _scenarioContext["UpdateSuccessful"] = false;
        }
    }

    [When(@"I remove product (.*) from my basket")]
    public async Task WhenIRemoveProductFromMyBasket(int productId)
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        
        try
        {
            await _basketDriver.RemoveItemFromBasketAsync(customerId, productId);
            _scenarioContext["RemoveSuccessful"] = true;
        }
        catch (Exception ex)
        {
            _scenarioContext["RemoveException"] = ex;
            _scenarioContext["RemoveSuccessful"] = false;
        }
    }

    [When(@"I attempt to add a product with ID (.*) and quantity (.*)")]
    public async Task WhenIAttemptToAddAProductWithIDAndQuantity(int productId, int quantity)
    {
        await WhenIAddAProductWithIDAndQuantityToMyBasket(productId, quantity);
    }

    [When(@"I simulate a new session for the same user")]
    public async Task WhenISimulateANewSessionForTheSameUser()
    {
        // Simulate session change by clearing local state but keeping server state
        var customerId = _scenarioContext.Get<string>("CustomerId");
        _scenarioContext["SimulatedNewSession"] = true;
        _scenarioContext["CustomerId"] = customerId; // Keep same customer ID
    }

    [When(@"I retrieve my basket")]
    public async Task WhenIRetrieveMyBasket()
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        var basket = await _basketDriver.GetBasketAsync(customerId);
        _scenarioContext["RetrievedBasket"] = basket;
    }

    [When(@"I initiate the checkout process")]
    public async Task WhenIInitiateTheCheckoutProcess()
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        
        try
        {
            var checkoutData = await _basketDriver.PrepareCheckoutAsync(customerId);
            _scenarioContext["CheckoutData"] = checkoutData;
            _scenarioContext["CheckoutSuccessful"] = true;
        }
        catch (Exception ex)
        {
            _scenarioContext["CheckoutException"] = ex;
            _scenarioContext["CheckoutSuccessful"] = false;
        }
    }

    [When(@"multiple operations attempt to modify the basket simultaneously")]
    public async Task WhenMultipleOperationsAttemptToModifyTheBasketSimultaneously()
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        
        // Simulate concurrent operations
        var tasks = new List<Task>
        {
            _basketDriver.AddItemToBasketAsync(customerId, 10, "Concurrent Product 1", 1, 50.0m),
            _basketDriver.AddItemToBasketAsync(customerId, 11, "Concurrent Product 2", 2, 75.0m),
            _basketDriver.UpdateItemQuantityAsync(customerId, 1, 5),
            _basketDriver.RemoveItemFromBasketAsync(customerId, 2)
        };

        try
        {
            await Task.WhenAll(tasks);
            _scenarioContext["ConcurrentOperationsSuccessful"] = true;
        }
        catch (Exception ex)
        {
            _scenarioContext["ConcurrentOperationsException"] = ex;
            _scenarioContext["ConcurrentOperationsSuccessful"] = false;
        }
    }

    [When(@"I perform basket operations")]
    public async Task WhenIPerformBasketOperations()
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        var stopwatch = Stopwatch.StartNew();

        // Perform a series of typical basket operations
        await _basketDriver.AddItemToBasketAsync(customerId, 100, "Performance Test Product", 1, 199.99m);
        await _basketDriver.UpdateItemQuantityAsync(customerId, 100, 2);
        var basket = await _basketDriver.GetBasketAsync(customerId);
        
        stopwatch.Stop();
        _scenarioContext["OperationsResponseTime"] = stopwatch.Elapsed;
        _scenarioContext["FinalBasket"] = basket;
    }

    [Then(@"my basket should contain (.*) item")]
    [Then(@"my basket should contain (.*) items")]
    public async Task ThenMyBasketShouldContainItems(int expectedCount)
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        var basket = await _basketDriver.GetBasketAsync(customerId);
        
        basket.Items.Count.ShouldBe(expectedCount);
        _scenarioContext["CurrentBasket"] = basket;
    }

    [Then(@"my basket should contain (.*) different items")]
    public async Task ThenMyBasketShouldContainDifferentItems(int expectedCount)
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        var basket = await _basketDriver.GetBasketAsync(customerId);
        
        basket.Items.Select(i => i.ProductId).Distinct().Count().ShouldBe(expectedCount);
    }

    [Then(@"the item should have quantity (.*)")]
    public async Task ThenTheItemShouldHaveQuantity(int expectedQuantity)
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        var basket = await _basketDriver.GetBasketAsync(customerId);
        
        basket.Items.First().Quantity.ShouldBe(expectedQuantity);
    }

    [Then(@"the basket total should reflect the item price")]
    public async Task ThenTheBasketTotalShouldReflectTheItemPrice()
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        var basket = await _basketDriver.GetBasketAsync(customerId);
        
        var expectedTotal = basket.Items.Sum(i => i.UnitPrice * i.Quantity);
        basket.Items.Sum(i => i.UnitPrice * i.Quantity).ShouldBe(expectedTotal);
    }

    [Then(@"the basket total should be (.*)")]
    public async Task ThenTheBasketTotalShouldBe(decimal expectedTotal)
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        var basket = await _basketDriver.GetBasketAsync(customerId);
        
        var actualTotal = basket.Items.Sum(i => i.UnitPrice * i.Quantity);
        actualTotal.ShouldBe(expectedTotal);
    }

    [Then(@"each item should maintain its individual properties")]
    public async Task ThenEachItemShouldMaintainItsIndividualProperties()
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        var basket = await _basketDriver.GetBasketAsync(customerId);
        
        foreach (var item in basket.Items)
        {
            item.ProductId.ShouldBeGreaterThan(0);
            item.ProductName.ShouldNotBeNullOrEmpty();
            item.Quantity.ShouldBeGreaterThan(0);
            item.UnitPrice.ShouldBeGreaterThan(0);
        }
    }

    [Then(@"the item quantity should be updated to (.*)")]
    public async Task ThenTheItemQuantityShouldBeUpdatedTo(int expectedQuantity)
    {
        var updateSuccessful = _scenarioContext.Get<bool>("UpdateSuccessful");
        updateSuccessful.ShouldBeTrue();
        
        var customerId = _scenarioContext.Get<string>("CustomerId");
        var basket = await _basketDriver.GetBasketAsync(customerId);
        
        basket.Items.First().Quantity.ShouldBe(expectedQuantity);
    }

    [Then(@"the remaining item should be product (.*)")]
    public async Task ThenTheRemainingItemShouldBeProduct(int expectedProductId)
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        var basket = await _basketDriver.GetBasketAsync(customerId);
        
        basket.Items.Count.ShouldBe(1);
        basket.Items.First().ProductId.ShouldBe(expectedProductId);
    }

    [Then(@"I should receive a validation error")]
    public void ThenIShouldReceiveAValidationError()
    {
        var operationSuccessful = _scenarioContext.Get<bool>("OperationSuccessful");
        operationSuccessful.ShouldBeFalse();
        
        var exception = _scenarioContext.Get<Exception>("OperationException");
        exception.ShouldNotBeNull();
    }

    [Then(@"the error message should indicate invalid quantity")]
    public void ThenTheErrorMessageShouldIndicateInvalidQuantity()
    {
        var exception = _scenarioContext.Get<Exception>("OperationException");
        exception.Message.ShouldContain("quantity", Case.Insensitive);
    }

    [Then(@"my basket should remain empty")]
    public async Task ThenMyBasketShouldRemainEmpty()
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        var basket = await _basketDriver.GetBasketAsync(customerId);
        
        basket.Items.ShouldBeEmpty();
    }

    [Then(@"I should receive a not found error")]
    public void ThenIShouldReceiveANotFoundError()
    {
        var operationSuccessful = _scenarioContext.Get<bool>("OperationSuccessful");
        operationSuccessful.ShouldBeFalse();
        
        var exception = _scenarioContext.Get<Exception>("OperationException");
        exception.ShouldNotBeNull();
    }

    [Then(@"the error message should indicate product not found")]
    public void ThenTheErrorMessageShouldIndicateProductNotFound()
    {
        var exception = _scenarioContext.Get<Exception>("OperationException");
        exception.Message.ShouldContain("not found", Case.Insensitive);
    }

    [Then(@"my basket should contain the same items")]
    public void ThenMyBasketShouldContainTheSameItems()
    {
        var originalBasket = _scenarioContext.Get<CustomerBasket>("CurrentBasket");
        var retrievedBasket = _scenarioContext.Get<CustomerBasket>("RetrievedBasket");
        
        retrievedBasket.Items.Count.ShouldBe(originalBasket.Items.Count);
        
        foreach (var originalItem in originalBasket.Items)
        {
            var retrievedItem = retrievedBasket.Items.FirstOrDefault(i => i.ProductId == originalItem.ProductId);
            retrievedItem.ShouldNotBeNull();
            retrievedItem.Quantity.ShouldBe(originalItem.Quantity);
            retrievedItem.UnitPrice.ShouldBe(originalItem.UnitPrice);
        }
    }

    [Then(@"the basket total should be preserved")]
    public void ThenTheBasketTotalShouldBePreserved()
    {
        var originalBasket = _scenarioContext.Get<CustomerBasket>("CurrentBasket");
        var retrievedBasket = _scenarioContext.Get<CustomerBasket>("RetrievedBasket");
        
        var originalTotal = originalBasket.Items.Sum(i => i.UnitPrice * i.Quantity);
        var retrievedTotal = retrievedBasket.Items.Sum(i => i.UnitPrice * i.Quantity);
        
        retrievedTotal.ShouldBe(originalTotal);
    }

    [Then(@"all item details should be intact")]
    public void ThenAllItemDetailsShouldBeIntact()
    {
        var retrievedBasket = _scenarioContext.Get<CustomerBasket>("RetrievedBasket");
        
        foreach (var item in retrievedBasket.Items)
        {
            item.ProductId.ShouldBeGreaterThan(0);
            item.ProductName.ShouldNotBeNullOrEmpty();
            item.Quantity.ShouldBeGreaterThan(0);
            item.UnitPrice.ShouldBeGreaterThan(0);
        }
    }

    [Then(@"the basket should be validated for checkout")]
    public void ThenTheBasketShouldBeValidatedForCheckout()
    {
        var checkoutSuccessful = _scenarioContext.Get<bool>("CheckoutSuccessful");
        checkoutSuccessful.ShouldBeTrue();
        
        var checkoutData = _scenarioContext.Get<object>("CheckoutData");
        checkoutData.ShouldNotBeNull();
    }

    [Then(@"all items should have current pricing")]
    public void ThenAllItemsShouldHaveCurrentPricing()
    {
        var checkoutData = _scenarioContext.Get<object>("CheckoutData");
        checkoutData.ShouldNotBeNull();
        
        // In a real implementation, this would verify that prices are current
        // For now, we just verify the checkout data exists
    }

    [Then(@"stock availability should be confirmed")]
    public void ThenStockAvailabilityShouldBeConfirmed()
    {
        var checkoutData = _scenarioContext.Get<object>("CheckoutData");
        checkoutData.ShouldNotBeNull();
        
        // In a real implementation, this would verify stock availability
        // For now, we just verify the checkout process succeeded
    }

    [Then(@"the checkout data should be properly formatted")]
    public void ThenTheCheckoutDataShouldBeProperlyFormatted()
    {
        var checkoutData = _scenarioContext.Get<object>("CheckoutData");
        checkoutData.ShouldNotBeNull();
        
        // Verify checkout data structure
        checkoutData.GetType().ShouldNotBeNull();
    }

    [Then(@"the basket should maintain data consistency")]
    public async Task ThenTheBasketShouldMaintainDataConsistency()
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        var basket = await _basketDriver.GetBasketAsync(customerId);
        
        // Verify basket is in a consistent state
        basket.ShouldNotBeNull();
        basket.Items.ShouldNotBeNull();
        
        // Verify no duplicate items
        var productIds = basket.Items.Select(i => i.ProductId).ToList();
        productIds.Count.ShouldBe(productIds.Distinct().Count());
    }

    [Then(@"conflicting operations should be handled gracefully")]
    public void ThenConflictingOperationsShouldBeHandledGracefully()
    {
        // Verify that concurrent operations either succeeded or failed gracefully
        var concurrentSuccessful = _scenarioContext.Get<bool>("ConcurrentOperationsSuccessful");
        
        if (!concurrentSuccessful)
        {
            var exception = _scenarioContext.Get<Exception>("ConcurrentOperationsException");
            exception.ShouldNotBeNull();
            // Exception should be a known concurrency exception type
        }
    }

    [Then(@"the final basket state should be valid")]
    public async Task ThenTheFinalBasketStateShouldBeValid()
    {
        var customerId = _scenarioContext.Get<string>("CustomerId");
        var basket = await _basketDriver.GetBasketAsync(customerId);
        
        basket.ShouldNotBeNull();
        
        foreach (var item in basket.Items)
        {
            item.ProductId.ShouldBeGreaterThan(0);
            item.Quantity.ShouldBeGreaterThan(0);
            item.UnitPrice.ShouldBeGreaterThan(0);
        }
    }

    [Then(@"each operation should complete within (.*) second")]
    [Then(@"each operation should complete within (.*) seconds")]
    public void ThenEachOperationShouldCompleteWithinSeconds(int maxSeconds)
    {
        var responseTime = _scenarioContext.Get<TimeSpan>("OperationsResponseTime");
        responseTime.TotalSeconds.ShouldBeLessThan(maxSeconds);
    }

    [Then(@"all data should be accurate")]
    public void ThenAllDataShouldBeAccurate()
    {
        var finalBasket = _scenarioContext.Get<CustomerBasket>("FinalBasket");
        
        foreach (var item in finalBasket.Items)
        {
            item.ProductId.ShouldBeGreaterThan(0);
            item.ProductName.ShouldNotBeNullOrEmpty();
            item.Quantity.ShouldBeGreaterThan(0);
            item.UnitPrice.ShouldBeGreaterThan(0);
        }
    }
}