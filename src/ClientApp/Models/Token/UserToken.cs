using System.Text.Json.Serialization;

namespace eShop.ClientApp.Models.Token;

public class UserToken
{
    [JsonPropertyName("IdToken")]
    public string IdToken { get; set; }

    [JsonPropertyName("AccessToken")]
    public string AccessToken { get; set; }

    [JsonPropertyName("ExpiresIn")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("TokenType")]
    public string TokenType { get; set; }

    [JsonPropertyName("RefreshToken")]
    public string RefreshToken { get; set; }
}
