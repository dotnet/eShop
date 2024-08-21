using System.Text.Json.Serialization;

namespace eShop.ClientApp.Models.User;

public class UserInfo
{
    [JsonPropertyName("sub")] public string UserId { get; set; }

    [JsonPropertyName("preferred_username")]
    public string PreferredUsername { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("last_name")] public string LastName { get; set; }

    [JsonPropertyName("card_number")] public string CardNumber { get; set; }

    [JsonPropertyName("card_holder")] public string CardHolder { get; set; }

    [JsonPropertyName("card_security_number")]
    public string CardSecurityNumber { get; set; }

    [JsonPropertyName("address_city")] public string Address { get; set; }

    [JsonPropertyName("address_country")] public string Country { get; set; }

    [JsonPropertyName("address_state")] public string State { get; set; }

    [JsonPropertyName("address_street")] public string Street { get; set; }

    [JsonPropertyName("address_zip_code")] public string ZipCode { get; set; }

    [JsonPropertyName("email")] public string Email { get; set; }

    [JsonPropertyName("email_verified")] public bool EmailVerified { get; set; }

    [JsonPropertyName("phone_number")] public string PhoneNumber { get; set; }

    [JsonPropertyName("phone_number_verified")]
    public bool PhoneNumberVerified { get; set; }

    public static UserInfo Default { get; } = new();
}
