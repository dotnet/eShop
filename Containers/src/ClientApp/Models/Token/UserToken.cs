using System.Text.Json.Serialization;

namespace eShop.ClientApp.Models.Token;

public class UserToken
{
    [JsonPropertyName("id_token")]
    public string IdToken { get; set; }

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }
}
