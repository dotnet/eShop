namespace ClientApp.UnitTests.Mocks;

public class MockNavigationService : INavigationService
{
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task NavigateToAsync(string route, IDictionary<string, object>? routeParameters = null)
    {
        return Task.CompletedTask;
    }

    public Task PopAsync()
    {
        return Task.CompletedTask;
    }
}

