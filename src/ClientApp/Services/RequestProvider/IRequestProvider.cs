namespace eShop.ClientApp.Services.RequestProvider;

public interface IRequestProvider
{
    Task<TResult> GetAsync<TResult>(string uri, string token = "");
    
    Task<TResponse> PostAsync<TRequest, TResponse>(string uri, TRequest data, string token = "", string header = "");
    
    Task<bool> PostAsync<TRequest>(string uri, TRequest data, string token = "", string header = "");

    Task<TResult> PostAsync<TResult>(string uri, string data, string clientId, string clientSecret);

    Task<TResult> PutAsync<TResult>(string uri, TResult data, string token = "", string header = "");

    Task DeleteAsync(string uri, string token = "");
}
