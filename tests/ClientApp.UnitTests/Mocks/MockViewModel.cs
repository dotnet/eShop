using eShop.ClientApp.Validations;
using eShop.ClientApp.ViewModels.Base;

namespace ClientApp.UnitTests.Mocks;

public class MockViewModel : ViewModelBase
{
    public ValidatableObject<string> Forename { get; } = new();

    public ValidatableObject<string> Surname { get; } = new();

    public MockViewModel(INavigationService navigationService)
        : base(navigationService)
    {
        Forename = new ValidatableObject<string>();
        Surname = new ValidatableObject<string>();

        Forename.Validations.Add(new IsNotNullOrEmptyRule<string> { ValidationMessage = "Forename is required." });
        Surname.Validations.Add(new IsNotNullOrEmptyRule<string> { ValidationMessage = "Surname name is required." });
    }

    public bool Validate()
    {
        bool isValidForename = Forename.Validate();
        bool isValidSurname = Surname.Validate();
        return isValidForename && isValidSurname;
    }
}
