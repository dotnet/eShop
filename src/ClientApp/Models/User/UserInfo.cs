using System.Text.Json.Serialization;

namespace eShop.ClientApp.Models.User;

public class UserInfo
{
    [JsonPropertyName("Sub")]
    public string UserId { get; set; }

    [JsonPropertyName("PreferredUsername")]
    public string PreferredUsername { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("LastName")]
    public string LastName { get; set; }

    [JsonPropertyName("CardNumber")]
    public string CardNumber { get; set; }

    [JsonPropertyName("CardHolder")]
    public string CardHolder { get; set; }

    [JsonPropertyName("CardSecurityNumber")]
    public string CardSecurityNumber { get; set; }

    [JsonPropertyName("AddressCity")]
    public string Address { get; set; }

    [JsonPropertyName("AddressCountry")]
    public string Country { get; set; }

    [JsonPropertyName("AddressState")]
    public string State { get; set; }

    [JsonPropertyName("AddressStreet")]
    public string Street { get; set; }

    [JsonPropertyName("AddressZipcode")]
    public string ZipCode { get; set; }

    [JsonPropertyName("Email")]
    public string Email { get; set; }

    [JsonPropertyName("EmailVerified")]
    public bool EmailVerified { get; set; }

    [JsonPropertyName("PhoneNumber")]
    public string PhoneNumber { get; set; }

    [JsonPropertyName("PhoneNumberVerified")]
    public bool PhoneNumberVerified { get; set; }
}
