using eShop.Catalog.API.Services;
using Microsoft.AspNetCore.Mvc;

public class CatalogServices(
    CatalogContext context,
    [FromServices] ICatalogAI catalogAI,
    IOptions<CatalogOptions> options,
    ILogger<CatalogServices> logger,
    [FromServices] ICatalogIntegrationEventService eventService)
{
    public CatalogContext Context { get; } = context;
    public ICatalogAI CatalogAI { get; } = catalogAI;
    public IOptions<CatalogOptions> Options { get; } = options;
    public ILogger<CatalogServices> Logger { get; } = logger;
    public ICatalogIntegrationEventService EventService { get; } = eventService;
};
