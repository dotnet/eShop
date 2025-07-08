namespace PaymentGateway
{
    public class StripePaymentProcessor
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        public StripePaymentProcessor(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        }
        public async Task<string> CreateChargeAsync(long amount, string currency, string sourceToken, string description)
        {
            var chargeData = new Dictionary<string, string>
            {
                { "amount", amount.ToString() },
                { "currency", currency },
                { "source", sourceToken },
                { "description", description }
            };
            var content = new FormUrlEncodedContent(chargeData);
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync("https://api.stripe.com/v1/charges", content);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Error: {e.Message}");
                throw;
            }
        }
    }
} 