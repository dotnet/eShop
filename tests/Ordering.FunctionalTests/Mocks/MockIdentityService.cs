using eShop.Ordering.API.Infrastructure.Services;

namespace eShop.Ordering.FunctionalTests.Mocks;

internal sealed class MockIdentityService : IIdentityService
{
    public string GetUserIdentity() => AutoAuthorizeMiddleware.IDENTITY_ID;

    public string GetUserName() => AutoAuthorizeMiddleware.IDENTITY_ID;
}
