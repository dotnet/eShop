using eShop.ClientApp.Models.User;

namespace eShop.ClientApp.Services.User;

public class UserMockService : IUserService
{
    private readonly UserInfo MockUserInfo = new()
    {
        UserId = Guid.NewGuid().ToString(),
        Name = "Jhon",
        LastName = "Doe",
        PreferredUsername = "Jdoe",
        Email = "jdoe@eshop.com",
        EmailVerified = true,
        PhoneNumber = "202-555-0165",
        PhoneNumberVerified = true,
        Address = "Seattle, WA",
        Street = "120 E 87th Street",
        ZipCode = "98101",
        Country = "United States",
        State = "Seattle",
        CardNumber = "378282246310005",
        CardHolder = "American Express",
        CardSecurityNumber = "1234",
    };

    public async Task<UserInfo> GetUserInfoAsync(string authToken)
    {
        await Task.Delay(10);
        return MockUserInfo;
    }
}
