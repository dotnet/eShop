using ClientApp.UnitTests.Mocks;

namespace ClientApp.UnitTests.ViewModels;

[TestClass]
public class MainViewModelTests
{
    private readonly INavigationService _navigationService;

    public MainViewModelTests()
    {
        _navigationService = new MockNavigationService();
    }

    [TestMethod]
    public void SettingsCommandIsNotNullWhenViewModelInstantiatedTest()
    {
        var mainViewModel = new MainViewModel(_navigationService);
        Assert.IsNotNull(mainViewModel.SettingsCommand);
    }

    [TestMethod]
    public void IsBusyPropertyIsFalseWhenViewModelInstantiatedTest()
    {
        var mainViewModel = new MainViewModel(_navigationService);
        Assert.IsFalse(mainViewModel.IsBusy);
    }
}
