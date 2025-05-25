using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Inked.Submission.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Inked.Submission.API;

[Authorize]
public static class SubmissionApi
{
    public static IEndpointRouteBuilder MapSubmissionApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/submission").WithApiVersionSet().HasApiVersion(1, 0);

        // Routes for creating, retrieving, and deleting submissions
        api.MapPost("/", CreateSubmission)
            .WithName("CreateSubmission")
            .WithSummary("Create a new submission")
            .WithDescription("Create a new submission with a picture upload")
            .WithTags("Submissions")
            .RequireAuthorization()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .Produces(StatusCodes.Status201Created)
            .DisableAntiforgery();

        api.MapGet("/{id:guid}", GetSubmissionById)
            .WithName("GetSubmissionById")
            .WithSummary("Get submission by ID")
            .WithDescription("Retrieve a submission by its ID")
            .WithTags("Submissions");

        api.MapDelete("/{id:guid}", DeleteSubmissionById)
            .WithName("DeleteSubmissionById")
            .WithSummary("Delete submission by ID")
            .WithDescription("Delete a submission by its ID")
            .WithTags("Submissions")
            .RequireAuthorization();

        api.MapGet("/check-title/{title}", CheckTitleUnique)
            .WithName("CheckTitleUnique")
            .WithSummary("Check if a title is unique")
            .WithDescription("Check if a submission title is unique")
            .WithTags("Submissions");

        api.MapGet("/art/{id:guid}", GetArt)
            .WithName("GetArt")
            .WithSummary("Get art for submission ID")
            .WithDescription("Retrieve the art submitted in the submission ID")
            .WithTags("Submissions", "Art");

        api.MapGet("/", GetAllSubmissions)
            .WithName("GetAllSubmissions")
            .WithSummary("Get all submissions with filtering and sorting")
            .WithDescription("Retrieve all submissions, allowing for filtering by card type and theme, and sorting by creation date, title, or card type.")
            .WithTags("Submissions")
            .Produces<List<Inked.Submission.API.Model.SubmissionSummaryDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // CardType endpoints
        api.MapGet("/card-types", GetCardTypes)
            .WithName("GetCardTypes")
            .WithSummary("Get all card types")
            .WithTags("CardTypes");

        api.MapPost("/card-types", CreateCardType)
            .WithName("CreateCardType")
            .WithSummary("Create a new card type")
            .WithTags("CardTypes");

        api.MapDelete("/card-types/{id:int}", DeleteCardType)
            .WithName("DeleteCardType")
            .WithSummary("Delete a card type by ID")
            .WithTags("CardTypes");

        // CardTheme endpoints
        api.MapGet("/card-themes", GetCardThemes)
            .WithName("GetCardThemes")
            .WithSummary("Get all card themes")
            .WithTags("CardThemes");

        api.MapPost("/card-themes", CreateCardTheme)
            .WithName("CreateCardTheme")
            .WithSummary("Create a new card theme")
            .WithTags("CardThemes");

        api.MapDelete("/card-themes/{id:int}", DeleteCardTheme)
            .WithName("DeleteCardTheme")
            .WithSummary("Delete a card theme by ID")
            .WithTags("CardThemes");

        return app;
    }

    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [Authorize]
    public static async Task<IResult> CreateSubmission(
        [AsParameters] SubmissionServices services,
        [FromForm] CreateSubmissionRequest request,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IStorageService storageService,
        [FromServices] IConfiguration configuration,
        [FromServices] ILogger<SubmissionServices> logger)
    {
        if (request.Art is null)
        {
            return Results.Json(new ProblemDetails { Detail = "Art file must be provided." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var maxFileSize = configuration.GetValue<long>("Kestrel:Limits:MaxRequestBodySize");
        if (request.Art.Length > maxFileSize)
        {
            return Results.Json(
                new ProblemDetails { Detail = $"File size must be under {maxFileSize / (1024 * 1024)}MB." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var user = httpContextAccessor?.HttpContext?.User;
        logger.LogInformation("Creating submission");
        logger.LogInformation("User {username}", user?.Identity?.Name);

        if (user?.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(user?.Identity?.Name))
        {
            return Results.Json(new ProblemDetails { Detail = "User must be authenticated to create a submission." },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var author = user.Identity.Name;

        var titleExists = await services.Context.Submissions.AnyAsync(s => s.Title == request.Title);
        if (titleExists)
        {
            return Results.Json(new ProblemDetails { Detail = "Title is already in use." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Validate CardTypeId
        var cardTypeExists = await services.Context.CardTypes.AnyAsync(ct => ct.Id == request.CardTypeId);
        if (!cardTypeExists)
        {
            return Results.Json(new ProblemDetails { Detail = "Invalid CardTypeId provided." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var submissionId = Guid.NewGuid();
        var pictureFileName = submissionId.ToString();

        await storageService.SaveFileAsync(request.Art, pictureFileName);

        // Fetch the themes by IDs
        var themes = await services.Context.CardThemes
            .Where(t => request.Themes.Contains(t.Id))
            .ToListAsync();

        if (themes.Count != request.Themes.Count)
        {
            return Results.Json(new ProblemDetails { Detail = "One or more themes not found." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var submission = new Model.Submission
        {
            Id = submissionId,
            Title = request.Title,
            Description = request.Description,
            CardTypeId = request.CardTypeId,
            Author = author,
            Artist = request.Artist,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CardThemes = themes // Associate the themes
        };

        services.Context.Submissions.Add(submission);
        await services.Context.SaveChangesAsync();

        return Results.Json(new { submission.Id }, statusCode: StatusCodes.Status201Created);
    }

    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public static async Task<Results<Ok<Model.Submission>, NotFound>> GetSubmissionById(
        [AsParameters] SubmissionServices services,
        [Description("The submission ID")] Guid id)
    {
        var submission = await services.Context.Submissions.FindAsync(id);

        if (submission is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(submission);
    }

    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [Authorize]
    public static async Task<Results<NoContent, NotFound>> DeleteSubmissionById(
        [AsParameters] SubmissionServices services,
        [Description("The submission ID")] Guid id)
    {
        var submission = await services.Context.Submissions.FindAsync(id);

        if (submission is null)
        {
            return TypedResults.NotFound();
        }

        services.Context.Submissions.Remove(submission);
        await services.Context.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok<bool>, BadRequest<ProblemDetails>>> CheckTitleUnique(
        [AsParameters] SubmissionServices services,
        [Description("The submission title")] string title)
    {
        var exists = await services.Context.Submissions.AnyAsync(s => s.Title == title);
        return TypedResults.Ok(!exists);
    }

    public static async Task<Results<FileStreamHttpResult, NotFound>> GetArt(
        [FromRoute] Guid id,
        [FromServices] IStorageService storageService)
    {
        var fileName = id.ToString();
        var stream = await storageService.GetFileAsync(fileName);
        if (stream == null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Stream(stream, "application/octet-stream", fileName);
    }

    public static async Task<Results<Ok<List<CardType>>, BadRequest<ProblemDetails>>> GetCardTypes(
        [AsParameters] SubmissionServices services)
    {
        var cardTypes = await services.Context.CardTypes.ToListAsync();
        return TypedResults.Ok(cardTypes);
    }

    public static async Task<Results<Created<CardType>, BadRequest<ProblemDetails>>> CreateCardType(
        [FromBody] CardType cardType,
        [AsParameters] SubmissionServices services)
    {
        services.Context.CardTypes.Add(cardType);
        await services.Context.SaveChangesAsync();
        return TypedResults.Created($"/api/submission/card-types/{cardType.Id}", cardType);
    }

    public static async Task<Results<NoContent, NotFound>> DeleteCardType(
        [FromRoute] int id,
        [AsParameters] SubmissionServices services)
    {
        var cardType = await services.Context.CardTypes.FindAsync(id);
        if (cardType == null)
        {
            return TypedResults.NotFound();
        }

        services.Context.CardTypes.Remove(cardType);
        await services.Context.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok<List<CardTheme>>, BadRequest<ProblemDetails>>> GetCardThemes(
        [AsParameters] SubmissionServices services)
    {
        var cardThemes = await services.Context.CardThemes.ToListAsync();
        return TypedResults.Ok(cardThemes);
    }

    public static async Task<Results<Created<CardTheme>, BadRequest<ProblemDetails>>> CreateCardTheme(
        [FromBody] CardTheme cardTheme,
        [AsParameters] SubmissionServices services)
    {
        services.Context.CardThemes.Add(cardTheme);
        await services.Context.SaveChangesAsync();
        return TypedResults.Created($"/api/submission/card-themes/{cardTheme.Id}", cardTheme);
    }

    public static async Task<Results<NoContent, NotFound>> DeleteCardTheme(
        [FromRoute] int id,
        [AsParameters] SubmissionServices services)
    {
        var cardTheme = await services.Context.CardThemes.FindAsync(id);
        if (cardTheme == null)
        {
            return TypedResults.NotFound();
        }

        services.Context.CardThemes.Remove(cardTheme);
        await services.Context.SaveChangesAsync();
        return TypedResults.NoContent();
    }
}

public class CreateSubmissionRequest
{
    [Required] public string Title { get; set; } = string.Empty;
    [Required] public string Description { get; set; } = string.Empty;
    [Required] public string Artist { get; set; } = string.Empty;

    [Required] public int CardTypeId { get; set; }

    [Required] public List<int> Themes { get; set; } = new();
    [Required] public IFormFile Art { get; set; }
}

public static async Task<IResult> GetAllSubmissions(
    [AsParameters] SubmissionServices services,
    [FromServices] IHttpContextAccessor httpContextAccessor,
    [FromQuery] string? sortBy,
    [FromQuery] string? sortOrder,
    [FromQuery] int? cardTypeId,
    [FromQuery] int? themeId)
{
    try
    {
        var query = services.Context.Submissions
            .Include(s => s.CardType)
            .Include(s => s.CardThemes)
            .AsQueryable();

        // Filtering
        if (cardTypeId.HasValue)
        {
            query = query.Where(s => s.CardTypeId == cardTypeId.Value);
        }

        if (themeId.HasValue)
        {
            query = query.Where(s => s.CardThemes.Any(th => th.Id == themeId.Value));
        }

        // Sorting
        // Default sort order
        var actualSortOrder = sortOrder?.ToLowerInvariant() ?? "desc";
        var actualSortBy = sortBy?.ToLowerInvariant() ?? "createdat";

        if (actualSortBy == "createdat" && string.IsNullOrEmpty(sortBy)) // Default to desc for createdAt if no sortBy is specified
        {
            actualSortOrder = "desc";
        }
        else if (string.IsNullOrEmpty(sortOrder)) // Default to asc for other sort fields if no sortOrder is specified
        {
            actualSortOrder = "asc";
        }


        switch (actualSortBy)
        {
            case "title":
                query = actualSortOrder == "desc"
                    ? query.OrderByDescending(s => s.Title)
                    : query.OrderBy(s => s.Title);
                break;
            case "cardtype":
                query = actualSortOrder == "desc"
                    ? query.OrderByDescending(s => s.CardType.Type)
                    : query.OrderBy(s => s.CardType.Type);
                break;
            case "createdat":
            default: // Default to CreatedAt
                query = actualSortOrder == "desc"
                    ? query.OrderByDescending(s => s.CreatedAt)
                    : query.OrderBy(s => s.CreatedAt);
                break;
        }

        var submissions = await query.ToListAsync();

        // Construct base URL for ImageUrl.
        // This attempts to get the base URL from the request.
        // In a real-world scenario with gateways/proxies, this might need more robust handling (e.g., from configuration).
        var request = httpContextAccessor.HttpContext?.Request;
        var baseUrl = $"{request?.Scheme}://{request?.Host}";


        var submissionSummaries = submissions.Select(s => new Inked.Submission.API.Model.SubmissionSummaryDto
        {
            Id = s.Id,
            Title = s.Title,
            Author = s.Author,
            Artist = s.Artist,
            CardTypeName = s.CardType?.Type ?? "N/A",
            ThemeNames = s.CardThemes?.Select(ct => ct.Theme).ToList() ?? new List<string>(),
            ImageUrl = $"{baseUrl}/api/submission/art/{s.Id}",
            CreatedAt = s.CreatedAt
        }).ToList();

        return Results.Ok(submissionSummaries);
    }
    catch (Exception ex)
    {
        services.Logger.LogError(ex, "Error retrieving submissions.");
        return Results.Problem("An unexpected error occurred while retrieving submissions.", statusCode: StatusCodes.Status500InternalServerError);
    }
}
