namespace eShop.ClientApp.Services.RequestProvider;

public interface IRequestProvider
{
    Task<TResult> GetAsync<TResult>(string uri, string token = "") where TResult : class;
    
    Task<TResponse> PostAsync<TRequest, TResponse>(string uri, TRequest data, string token = "", string header = "") where TResponse : class;
    
    Task<bool> PostAsync<TRequest>(string uri, TRequest data, string token = "", string header = "") where TRequest : class;

    Task<TResult> PostAsync<TResult>(string uri, string data, string clientId, string clientSecret) where TResult : class;

    Task<TResult> PutAsync<TResult>(string uri, TResult data, string token = "", string header = "") where TResult : class;

    Task DeleteAsync(string uri, string token = "");
}
