using System.Text.Json;
using Inked.Submission.API.Services;

namespace Inked.Submission.API.Infrastructure;

public class SubmissionContextSeed(
    IWebHostEnvironment env,
    IOptions<SubmissionOptions> settings,
    ILogger<SubmissionContextSeed> logger) : IDbSeeder<SubmissionContext>
{
    public async Task SeedAsync(SubmissionContext context)
    {
        var useCustomizationData = settings.Value.UseCustomizationData;
        var contentRootPath = env.ContentRootPath;

        context.Database.OpenConnection();
        ((NpgsqlConnection)context.Database.GetDbConnection()).ReloadTypes();

        // Seed CardThemes
        var themesPath = Path.Combine(contentRootPath, "Setup", "themes.json");
        if (File.Exists(themesPath))
        {
            var themeJson = File.ReadAllText(themesPath);
            var themes = JsonSerializer.Deserialize<List<CardTheme>>(themeJson) ?? [];

            context.CardThemes.RemoveRange(context.CardThemes);
            await context.CardThemes.AddRangeAsync(themes);
            logger.LogInformation("Seeded {Count} card themes", themes.Count);
        }
        else
        {
            logger.LogWarning("themes.json not found at {Path}", themesPath);
        }

        // Seed CardTypes
        var typesPath = Path.Combine(contentRootPath, "Setup", "types.json");
        if (File.Exists(typesPath))
        {
            var typesJson = File.ReadAllText(typesPath);
            var types = JsonSerializer.Deserialize<List<CardType>>(typesJson) ?? [];

            context.CardTypes.RemoveRange(context.CardTypes);
            await context.CardTypes.AddRangeAsync(types);
            logger.LogInformation("Seeded {Count} card types", types.Count);
        }
        else
        {
            logger.LogWarning("types.json not found at {Path}", typesPath);
        }

        // Seed MintingType
        context.MintingTypes.RemoveRange(context.MintingTypes);
        var defaultMintingType = new MintingType { Id = 0, Type = "Submission" };
        await context.MintingTypes.AddAsync(defaultMintingType);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded default MintingType: {Type}", defaultMintingType.Type);
    }
}
