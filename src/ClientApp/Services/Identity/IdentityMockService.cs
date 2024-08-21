using eShop.ClientApp.Models.User;

namespace eShop.ClientApp.Services.Identity;

public class IdentityMockService : IIdentityService
{
    private bool _signedIn;

    public Task<bool> SignInAsync()
    {
        _signedIn = true;
        return Task.FromResult(_signedIn);
    }

    public Task<bool> SignOutAsync()
    {
        _signedIn = false;
        return Task.FromResult(_signedIn);
    }

    public Task<UserInfo> GetUserInfoAsync()
    {
        if (!_signedIn)
        {
            return Task.FromResult(UserInfo.Default);
        }

        return Task.FromResult(new UserInfo
        {
            UserId = Guid.NewGuid().ToString(),
            PreferredUsername = "sampleUser",
            Name = "Sample",
            LastName = "User",
            CardNumber = "XXXXXXXXXXXX3456",
            CardHolder = "Sample User",
            CardSecurityNumber = "123",
            Address = "123 Sample Street",
            Country = "USA",
            State = "Washington",
            Street = "123 Sample Street",
            ZipCode = "12345",
            Email = "sample.user@example.com",
            EmailVerified = true,
            PhoneNumber = "1234567890",
            PhoneNumberVerified = true
        });
    }

    public Task<string> GetAuthTokenAsync()
    {
        if (!_signedIn)
        {
            return Task.FromResult(string.Empty);
        }

        return Task.FromResult(Guid.NewGuid().ToString());
    }
}
