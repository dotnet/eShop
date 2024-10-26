using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eShop.ServiceDefaults
{
    public static class CJTokenExtension
    {

        public static IServiceCollection AddCJAuth(this IHostApplicationBuilder builder)
        {
            var services = builder.Services;

            services.AddSingleton<TokenService>();
            return services;
        }
    }



    public interface ITokenService
    {
        Task<string?> GetTokenAsync();
    }

    public class TokenService : ITokenService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TokenService> _logger;
        public string? token;
        public string? refreshToken;
        private readonly IConfiguration _configuration;
        public DateTime tokenExpiration;

        public TokenService(IHttpClientFactory httpClientFactory, ILogger<TokenService> logger, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<string?> GetTokenAsync()
        {
            if (token == null || DateTime.UtcNow >= tokenExpiration)
            {
                token = await FetchNewTokenAsync();
            }
            return token;
        }

        private async Task<string?> FetchNewTokenAsync()
        {
            // Logic to fetch a new token from the third-party API
            var client = _httpClientFactory.CreateClient();
            var data = "";

            // Rewrite if we wanna make admin work for different accounts
            var apiEmail = _configuration["CJTokenSettings:email"];
            var apiPassword = _configuration["CJTokenSettings:password"];
            var credentials = new { email = apiEmail, password = apiPassword };

            var serealizedCred = JsonSerializer.Serialize(credentials);
            var response = await client.PostAsync("https://developers.cjdropshipping.com/api2.0/v1/authentication/getAccessToken", new StringContent(serealizedCred, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();


            var jsonResponse = await response.Content.ReadAsStringAsync();

            using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
            {
                JsonElement root = doc.RootElement;

                // Extract the "data" field
                if (root.TryGetProperty("data", out JsonElement dataElement))
                {
                    data = dataElement.GetRawText();
                    Console.WriteLine($"Data field: {data}");
                }
                else
                {
                    Console.WriteLine("Data field not found.");
                }
            }
            var tokenJson = JsonSerializer.Deserialize<TokenResponse>(data);
            if (tokenJson != null)
            {
                //tokenExpiration = DateTime.UtcNow.AddSeconds(tokenJson.ExpiresIn);
                refreshToken = tokenJson.RefreshToken;
                return tokenJson.AccessToken;
            }

            return null;
        }
    }

    public class TokenResponse
    {
        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("accessTokenExpiryDate")]
        public string? AccessTokenExpirity { get; set; }
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }
    }

}
