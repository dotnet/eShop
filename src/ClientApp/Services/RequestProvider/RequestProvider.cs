using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using eShop.ClientApp.Exceptions;

namespace eShop.ClientApp.Services.RequestProvider;

public class RequestProvider(HttpMessageHandler _messageHandler) : IRequestProvider
{
    private readonly Lazy<HttpClient> _httpClient =
        new(() =>
            {
                var httpClient = _messageHandler is not null ? new HttpClient(_messageHandler) : new HttpClient();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                return httpClient;
            },
            LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly JsonSerializerOptions _jsonSerializerContext =
        new()
        {
            PropertyNameCaseInsensitive = true, 
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };

    public async Task<TResult> GetAsync<TResult>(string uri, string token = "")
    {
        var httpClient = GetOrCreateHttpClient(token);
        using var response = await httpClient.GetAsync(uri).ConfigureAwait(false);

        await HandleResponse(response).ConfigureAwait(false);

        var result = await response.Content.ReadFromJsonAsync<TResult>(_jsonSerializerContext).ConfigureAwait(false);

        return result;
    }

    public async Task<TResult> PostAsync<TRequest, TResult>(string uri, TRequest data, string token = "",
        string header = "")
    {
        var httpClient = GetOrCreateHttpClient(token);

        if (!string.IsNullOrEmpty(header))
        {
            AddHeaderParameter(httpClient, header);
        }
        
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(uri, data).ConfigureAwait(false);

        await HandleResponse(response).ConfigureAwait(false);
        var result = await response.Content.ReadFromJsonAsync<TResult>(_jsonSerializerContext).ConfigureAwait(false);

        return result;
    }

    public async Task<bool> PostAsync<TRequest>(string uri, TRequest data, string token = "", string header = "")
    {
        var httpClient = GetOrCreateHttpClient(token);

        if (!string.IsNullOrEmpty(header))
        {
            AddHeaderParameter(httpClient, header);
        }

        using var response = await httpClient.PostAsJsonAsync(uri, data).ConfigureAwait(false);

        await HandleResponse(response).ConfigureAwait(false);

        return response.IsSuccessStatusCode;
    }

    public async Task<TResult> PostAsync<TResult>(string uri, string data, string clientId, string clientSecret)
    {
        var httpClient = GetOrCreateHttpClient(string.Empty);

        if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret))
        {
            AddBasicAuthenticationHeader(httpClient, clientId, clientSecret);
        }

        using var content = new StringContent(data);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        using var response = await httpClient.PostAsync(uri, content).ConfigureAwait(false);

        await HandleResponse(response).ConfigureAwait(false);
        var result = await response.Content.ReadFromJsonAsync<TResult>(_jsonSerializerContext).ConfigureAwait(false);

        return result;
    }

    public async Task<TResult> PutAsync<TResult>(string uri, TResult data, string token = "", string header = "")
    {
        var httpClient = GetOrCreateHttpClient(token);

        if (!string.IsNullOrEmpty(header))
        {
            AddHeaderParameter(httpClient, header);
        }
        
        using HttpResponseMessage response = await httpClient.PutAsJsonAsync(uri, data).ConfigureAwait(false);

        await HandleResponse(response).ConfigureAwait(false);
        var result = await response.Content.ReadFromJsonAsync<TResult>(_jsonSerializerContext).ConfigureAwait(false);

        return result;
    }

    public async Task DeleteAsync(string uri, string token = "")
    {
        var httpClient = GetOrCreateHttpClient(token);
        await httpClient.DeleteAsync(uri).ConfigureAwait(false);
    }

    private HttpClient GetOrCreateHttpClient(string token = "")
    {
        var httpClient = _httpClient.Value;

        httpClient.DefaultRequestHeaders.Authorization =
            !string.IsNullOrEmpty(token)
                ? new AuthenticationHeaderValue("Bearer", token)
                : null;

        return httpClient;
    }

    private static void AddHeaderParameter(HttpClient httpClient, string parameter)
    {
        if (httpClient == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(parameter))
        {
            return;
        }

        httpClient.DefaultRequestHeaders.Add(parameter, Guid.NewGuid().ToString());
    }

    private static void AddBasicAuthenticationHeader(HttpClient httpClient, string clientId, string clientSecret)
    {
        if (httpClient == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return;
        }

        httpClient.DefaultRequestHeaders.Authorization = new BasicAuthenticationHeaderValue(clientId, clientSecret);
    }

    private static async Task HandleResponse(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new ServiceAuthenticationException(content);
            }

            throw new HttpRequestExceptionEx(response.StatusCode, content);
        }
    }
}
