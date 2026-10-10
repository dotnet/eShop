using ClientApp.UnitTests.Mocks;

namespace ClientApp.UnitTests.ViewModels;

[TestClass]
public class MockViewModelTests
{
    private readonly INavigationService _navigationService;

    public MockViewModelTests()
    {
        _navigationService = new MockNavigationService();
    }

    [TestMethod]
    public void CheckValidationFailsWhenPropertiesAreEmptyTest()
    {
        var mockViewModel = new MockViewModel(_navigationService);

        bool isValid = mockViewModel.Validate();

        Assert.IsFalse(isValid);
        Assert.IsNull(mockViewModel.Forename.Value);
        Assert.IsNull(mockViewModel.Surname.Value);
        Assert.IsFalse(mockViewModel.Forename.IsValid);
        Assert.IsFalse(mockViewModel.Surname.IsValid);
        Assert.AreNotEqual(0, mockViewModel.Forename.Errors.Count());
        Assert.AreNotEqual(0, mockViewModel.Surname.Errors.Count());
    }

    [TestMethod]
    public void CheckValidationFailsWhenOnlyForenameHasDataTest()
    {
        var mockViewModel = new MockViewModel(_navigationService);
        mockViewModel.Forename.Value = "John";

        bool isValid = mockViewModel.Validate();

        Assert.IsFalse(isValid);
        Assert.IsNotNull(mockViewModel.Forename.Value);
        Assert.IsNull(mockViewModel.Surname.Value);
        Assert.IsTrue(mockViewModel.Forename.IsValid);
        Assert.IsFalse(mockViewModel.Surname.IsValid);
        Assert.AreEqual(0, mockViewModel.Forename.Errors.Count());
        Assert.AreNotEqual(0, mockViewModel.Surname.Errors.Count());
    }

    [TestMethod]
    public void CheckValidationPassesWhenOnlySurnameHasDataTest()
    {
        var mockViewModel = new MockViewModel(_navigationService);
        mockViewModel.Surname.Value = "Smith";

        bool isValid = mockViewModel.Validate();

        Assert.IsFalse(isValid);
        Assert.IsNull(mockViewModel.Forename.Value);
        Assert.IsNotNull(mockViewModel.Surname.Value);
        Assert.IsFalse(mockViewModel.Forename.IsValid);
        Assert.IsTrue(mockViewModel.Surname.IsValid);
        Assert.AreNotEqual(0, mockViewModel.Forename.Errors.Count());
        Assert.AreEqual(0, mockViewModel.Surname.Errors.Count());
    }

    [TestMethod]
    public void CheckValidationPassesWhenBothPropertiesHaveDataTest()
    {
        var mockViewModel = new MockViewModel(_navigationService);
        mockViewModel.Forename.Value = "John";
        mockViewModel.Surname.Value = "Smith";

        bool isValid = mockViewModel.Validate();

        Assert.IsTrue(isValid);
        Assert.IsNotNull(mockViewModel.Forename.Value);
        Assert.IsNotNull(mockViewModel.Surname.Value);
        Assert.IsTrue(mockViewModel.Forename.IsValid);
        Assert.IsTrue(mockViewModel.Surname.IsValid);
        Assert.AreEqual(0, mockViewModel.Forename.Errors.Count());
        Assert.AreEqual(0, mockViewModel.Surname.Errors.Count());
    }

    [TestMethod]
    public void SettingForenamePropertyShouldRaisePropertyChanged()
    {
        bool invoked = false;
        var mockViewModel = new MockViewModel(_navigationService);

        mockViewModel.Forename.PropertyChanged += (_, e) =>
        {
            if (e?.PropertyName?.Equals(nameof(mockViewModel.Forename.Value)) ?? false)
            {
                invoked = true;
            }
        };
        mockViewModel.Forename.Value = "John";

        Assert.IsTrue(invoked);
    }

    [TestMethod]
    public void SettingSurnamePropertyShouldRaisePropertyChanged()
    {
        bool invoked = false;
        var mockViewModel = new MockViewModel(_navigationService);

        mockViewModel.Surname.PropertyChanged += (_, e) =>
        {
            if (e?.PropertyName?.Equals(nameof(mockViewModel.Surname.Value)) ?? false)
            {
                invoked = true;
            }
        };
        mockViewModel.Surname.Value = "Smith";

        Assert.IsTrue(invoked);
    }
}
