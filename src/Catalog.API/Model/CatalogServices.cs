using eShop.Catalog.API.Services;
using Microsoft.AspNetCore.Mvc;

public class CatalogServices(
    [FromServices] ICatalogRepository repository,
    [FromServices] ICatalogAI catalogAI,
    [FromServices] IOptions<CatalogOptions> options,
    [FromServices] ILogger<CatalogServices> logger,
    [FromServices] ICatalogIntegrationEventService eventService)
{
    public ICatalogRepository Repository { get; } = repository;
    public ICatalogAI CatalogAI { get; } = catalogAI;
    public IOptions<CatalogOptions> Options { get; } = options;
    public ILogger<CatalogServices> Logger { get; } = logger;
    public ICatalogIntegrationEventService EventService { get; } = eventService;
};
