using eShop.Ordering.Infrastructure.Idempotency;

namespace eShop.Ordering.FunctionalTests.Mocks.RepositoryMock;

internal sealed class InMemoryRequestManager : IRequestManager
{
    private readonly HashSet<Guid> _requests = [];

    public Task<bool> ExistAsync(Guid id) => Task.FromResult(_requests.Contains(id));

    public Task CreateRequestForCommandAsync<T>(Guid id)
    {
        _requests.Add(id);
        return Task.CompletedTask;
    }
}
