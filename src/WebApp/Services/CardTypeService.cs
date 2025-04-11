namespace Inked.WebApp.Services;

public class CardTypeService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CardTypeService> _logger;

    public CardTypeService(HttpClient httpClient, ILogger<CardTypeService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<CardType>?> GetCardTypesAsync()
    {
        try
        {
            _logger.LogInformation("Fetching card types.");
            return await _httpClient.GetFromJsonAsync<List<CardType>>("api/submission/card-types");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching card types.");
            return null;
        }
    }

    public async Task<bool> CreateCardTypeAsync(CardType cardType)
    {
        try
        {
            _logger.LogInformation("Creating a new card type.");
            var response = await _httpClient.PostAsJsonAsync("api/submission/card-types", cardType);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating card type.");
            return false;
        }
    }

    public async Task<bool> DeleteCardTypeAsync(int id)
    {
        try
        {
            _logger.LogInformation("Deleting card type with ID {Id}.", id);
            var response = await _httpClient.DeleteAsync($"api/submission/card-types/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting card type.");
            return false;
        }
    }
}

public class CardType
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
