namespace eShop.ClientApp.UnitTests;

public class MainViewModelTests
{
    private readonly INavigationService _navigationService;

    public MainViewModelTests()
    {
        _navigationService = new MockNavigationService();
    }

    [Fact]
    public void SettingsCommandIsNotNullWhenViewModelInstantiatedTest()
    {
        var mainViewModel = new MainViewModel(_navigationService);
        Assert.NotNull(mainViewModel.SettingsCommand);
    }

    [Fact]
    public void IsBusyPropertyIsFalseWhenViewModelInstantiatedTest()
    {
        var mainViewModel = new MainViewModel(_navigationService);
        Assert.False(mainViewModel.IsBusy);
    }
}
