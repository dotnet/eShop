public class OrderServices(
    OrderApplicationService application,
    IOrderQueries queries,
    IIdentityService identityService,
    ILogger<OrderServices> logger)
{
    public OrderApplicationService Application { get; } = application;
    public ILogger<OrderServices> Logger { get; } = logger;
    public IOrderQueries Queries { get; } = queries;
    public IIdentityService IdentityService { get; } = identityService;
}
