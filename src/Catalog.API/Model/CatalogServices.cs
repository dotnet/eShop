using System.Net.Http;
using eShop.Catalog.API.Services;

public class CatalogServices(
    CatalogContext context,
    ICatalogAI catalogAI,
    IOptions<CatalogOptions> options,
    ILogger<CatalogServices> logger,
    ICatalogIntegrationEventService eventService,
    CJCatalog utils,
    HttpClient httpClient,
    TokenService tokenService
    )
{
    public CatalogContext Context { get; } = context;
    public ICatalogAI CatalogAI { get; } = catalogAI;
    public IOptions<CatalogOptions> Options { get; } = options;
    public ILogger<CatalogServices> Logger { get; } = logger;
    public ICatalogIntegrationEventService EventService { get; } = eventService;

    public CJCatalog Utils { get; } = utils;

    public HttpClient HttpClient { get; } = httpClient;

    public TokenService TokenService { get; } = tokenService;
};
