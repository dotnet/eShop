namespace Inked.WebApp.Services;

public class CardThemeService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CardThemeService> _logger;

    public CardThemeService(HttpClient httpClient, ILogger<CardThemeService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<CardTheme>?> GetCardThemesAsync()
    {
        try
        {
            _logger.LogInformation("Fetching card themes.");
            return await _httpClient.GetFromJsonAsync<List<CardTheme>>("api/submission/card-themes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching card themes.");
            return null;
        }
    }

    public async Task<bool> CreateCardThemeAsync(CardTheme cardTheme)
    {
        try
        {
            _logger.LogInformation("Creating a new card theme.");
            var response = await _httpClient.PostAsJsonAsync("api/submission/card-themes", cardTheme);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating card theme.");
            return false;
        }
    }

    public async Task<bool> DeleteCardThemeAsync(int id)
    {
        try
        {
            _logger.LogInformation("Deleting card theme with ID {Id}.", id);
            var response = await _httpClient.DeleteAsync($"api/submission/card-themes/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting card theme.");
            return false;
        }
    }
}

public class CardTheme
{
    public int Id { get; set; }
    public string Theme { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
