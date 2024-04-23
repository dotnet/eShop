namespace ClientApp.UnitTests.Mocks;

public class MockDialogService : IDialogService
{
    public Task ShowAlertAsync(string message, string title, string buttonLabel)
    {
        return Task.CompletedTask;
    }
}

