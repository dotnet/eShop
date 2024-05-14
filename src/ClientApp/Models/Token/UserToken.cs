using System.Text.Json.Serialization;

namespace eShop.ClientApp.Models.Token;

public class UserToken
{
    [JsonPropertyName("id_token")] public string IdToken { get; set; }

    [JsonPropertyName("access_token")] public string AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }
    
    [JsonPropertyName("expires_at")]
    public DateTimeOffset ExpiresAt { get; set; }
}
