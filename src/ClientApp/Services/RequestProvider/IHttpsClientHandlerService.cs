namespace eShop.ClientApp.Services.RequestProvider;
public interface IHttpsClientHandlerService
{
    public HttpMessageHandler GetPlatformMessageHandler();
}
