using Microsoft.AspNetCore.Mvc;

namespace Inked.Submission.API.Services;

public class SubmissionServices(
    SubmissionContext context,
    [FromServices] ISubmissionAI catalogAI,
    IOptions<SubmissionOptions> options,
    ILogger<SubmissionServices> logger,
    [FromServices] ISubmissionIntegrationEventService eventService,
    IWebHostEnvironment environment)
{
    public SubmissionContext Context { get; } = context;
    public ISubmissionAI CatalogAI { get; } = catalogAI;
    public IOptions<SubmissionOptions> Options { get; } = options;
    public ILogger<SubmissionServices> Logger { get; } = logger;
    public ISubmissionIntegrationEventService EventService { get; } = eventService;
    public IWebHostEnvironment Environment { get; } = environment;
}
