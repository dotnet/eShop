public class OrderServices(
    IOrderQueries queries,
    IIdentityService identityService,
    ILogger<OrderServices> logger)
{
    public ILogger<OrderServices> Logger { get; } = logger;
    public IOrderQueries Queries { get; } = queries;
    public IIdentityService IdentityService { get; } = identityService;
}
