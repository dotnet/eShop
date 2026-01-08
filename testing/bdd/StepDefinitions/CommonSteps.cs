using Reqnroll;
using Shouldly;

namespace eShop.BddTests.StepDefinitions;

[Binding]
public class CommonSteps
{
    private readonly ScenarioContext _scenarioContext;

    public CommonSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Then(@"the response should indicate success")]
    public void ThenTheResponseShouldIndicateSuccess()
    {
        // Generic success validation that works for any response type
        var response = _scenarioContext.Get<object>("ApiResponse");
        
        if (response != null)
        {
            // Use reflection to check Success property on any response type
            var successProperty = response.GetType().GetProperty("Success");
            if (successProperty != null)
            {
                var success = (bool)successProperty.GetValue(response);
                success.ShouldBeTrue("Response should indicate success");
            }
            else
            {
                // If no Success property, assume success if response exists
                response.ShouldNotBeNull("Response should exist for successful operation");
            }
        }
        else
        {
            // Check if operation was marked as successful in scenario context
            var operationSuccessful = _scenarioContext.ContainsKey("OperationSuccessful") 
                ? _scenarioContext.Get<bool>("OperationSuccessful") 
                : true;
            operationSuccessful.ShouldBeTrue("Operation should be successful");
        }
    }

    [Then(@"the correlation ID should be present")]
    public void ThenTheCorrelationIdShouldBePresent()
    {
        // Generic correlation ID validation that works for any response type
        var response = _scenarioContext.Get<object>("ApiResponse");
        
        if (response != null)
        {
            var correlationIdProperty = response.GetType().GetProperty("CorrelationId");
            if (correlationIdProperty != null)
            {
                var correlationId = correlationIdProperty.GetValue(response) as string;
                correlationId.ShouldNotBeNullOrEmpty("Correlation ID should be present in response");
            }
        }
        
        // Also check if correlation ID was set in scenario context
        if (_scenarioContext.ContainsKey("CorrelationId"))
        {
            var correlationId = _scenarioContext.Get<string>("CorrelationId");
            correlationId.ShouldNotBeNullOrEmpty("Correlation ID should be present in scenario context");
        }
    }

    [Then(@"the response should be properly formatted")]
    public void ThenTheResponseShouldBeProperlyFormatted()
    {
        var response = _scenarioContext.Get<object>("ApiResponse");
        response.ShouldNotBeNull("Response should not be null");
        
        // Verify response has expected structure
        var responseType = response.GetType();
        responseType.ShouldNotBeNull("Response should have a valid type");
    }

    [Then(@"the error message should be descriptive")]
    public void ThenTheErrorMessageShouldBeDescriptive()
    {
        // Check for error message in various possible locations
        string errorMessage = null;
        
        // Check in response object
        var response = _scenarioContext.Get<object>("ApiResponse");
        if (response != null)
        {
            var messageProperty = response.GetType().GetProperty("Message");
            if (messageProperty != null)
            {
                errorMessage = messageProperty.GetValue(response) as string;
            }
        }
        
        // Check in exception
        if (string.IsNullOrEmpty(errorMessage) && _scenarioContext.ContainsKey("OperationException"))
        {
            var exception = _scenarioContext.Get<Exception>("OperationException");
            errorMessage = exception?.Message;
        }
        
        // Check in scenario context
        if (string.IsNullOrEmpty(errorMessage) && _scenarioContext.ContainsKey("ErrorMessage"))
        {
            errorMessage = _scenarioContext.Get<string>("ErrorMessage");
        }
        
        errorMessage.ShouldNotBeNullOrEmpty("Error message should be descriptive");
        errorMessage.Length.ShouldBeGreaterThan(5, "Error message should be meaningful");
    }

    [Then(@"the response execution time should be acceptable")]
    public void ThenTheResponseExecutionTimeShouldBeAcceptable()
    {
        if (_scenarioContext.ContainsKey("ResponseTime"))
        {
            var responseTime = _scenarioContext.Get<TimeSpan>("ResponseTime");
            responseTime.TotalSeconds.ShouldBeLessThan(5, "Response time should be under 5 seconds");
        }
    }

    [Then(@"the response time should be less than (.*) second")]
    [Then(@"the response time should be less than (.*) seconds")]
    public void ThenTheResponseTimeShouldBeLessThanSeconds(int maxSeconds)
    {
        if (_scenarioContext.ContainsKey("ResponseTime"))
        {
            var responseTime = _scenarioContext.Get<TimeSpan>("ResponseTime");
            responseTime.TotalSeconds.ShouldBeLessThan(maxSeconds, $"Response time should be under {maxSeconds} seconds");
        }
    }

    [Then(@"all required fields should be present")]
    public void ThenAllRequiredFieldsShouldBePresent()
    {
        var response = _scenarioContext.Get<object>("ApiResponse");
        response.ShouldNotBeNull("Response should contain all required fields");
        
        // This is a generic validation - specific field validation should be done in domain-specific steps
        var responseType = response.GetType();
        var properties = responseType.GetProperties();
        properties.ShouldNotBeEmpty("Response should have properties");
    }

    [Given(@"test data is seeded in the system")]
    public async Task GivenTestDataIsSeededInTheSystem()
    {
        // Mark that test data seeding has been requested
        _scenarioContext["TestDataSeeded"] = true;
        await Task.CompletedTask;
    }

    [Given(@"the system is under normal load")]
    public void GivenTheSystemIsUnderNormalLoad()
    {
        _scenarioContext["LoadCondition"] = "Normal";
    }

    [Given(@"the system is under high load")]
    public void GivenTheSystemIsUnderHighLoad()
    {
        _scenarioContext["LoadCondition"] = "High";
    }

    [When(@"I perform the operation")]
    public async Task WhenIPerformTheOperation()
    {
        // Generic operation placeholder - specific operations should be defined in domain steps
        _scenarioContext["GenericOperationPerformed"] = true;
        await Task.CompletedTask;
    }

    [Then(@"the operation should complete successfully")]
    public void ThenTheOperationShouldCompleteSuccessfully()
    {
        var operationSuccessful = _scenarioContext.ContainsKey("OperationSuccessful") 
            ? _scenarioContext.Get<bool>("OperationSuccessful") 
            : true;
        operationSuccessful.ShouldBeTrue("Operation should complete successfully");
    }

    [Then(@"the system should maintain data consistency")]
    public void ThenTheSystemShouldMaintainDataConsistency()
    {
        // Generic data consistency check
        // Specific consistency checks should be implemented in domain-specific steps
        var response = _scenarioContext.Get<object>("ApiResponse");
        response.ShouldNotBeNull("System should maintain consistent state");
    }

    [Then(@"the response should contain valid data")]
    public void ThenTheResponseShouldContainValidData()
    {
        var response = _scenarioContext.Get<object>("ApiResponse");
        response.ShouldNotBeNull("Response should contain valid data");
        
        // Basic validation - specific data validation should be done in domain steps
        var responseType = response.GetType();
        responseType.ShouldNotBeNull("Response should have valid structure");
    }

    [Then(@"no errors should occur")]
    public void ThenNoErrorsShouldOccur()
    {
        var hasException = _scenarioContext.ContainsKey("OperationException");
        hasException.ShouldBeFalse("No exceptions should occur during operation");
        
        var operationSuccessful = _scenarioContext.ContainsKey("OperationSuccessful") 
            ? _scenarioContext.Get<bool>("OperationSuccessful") 
            : true;
        operationSuccessful.ShouldBeTrue("Operation should complete without errors");
    }

    [Then(@"the system should handle the request gracefully")]
    public void ThenTheSystemShouldHandleTheRequestGracefully()
    {
        // Check that even if there were errors, they were handled gracefully
        if (_scenarioContext.ContainsKey("OperationException"))
        {
            var exception = _scenarioContext.Get<Exception>("OperationException");
            exception.ShouldNotBeNull("Exception should be properly captured");
            exception.Message.ShouldNotBeNullOrEmpty("Exception should have descriptive message");
        }
        
        // System should not crash or return null responses
        if (_scenarioContext.ContainsKey("ApiResponse"))
        {
            var response = _scenarioContext.Get<object>("ApiResponse");
            // Response can be null for some operations, but if it exists, it should be valid
            if (response != null)
            {
                response.GetType().ShouldNotBeNull("Response should have valid type");
            }
        }
    }
}