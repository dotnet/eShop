using System.Net;

namespace eShop.ClientApp.Services.RequestProvider;

public class HttpRequestExceptionEx : HttpRequestException
{
    public HttpRequestExceptionEx(HttpStatusCode code) : this(code, null, null)
    {
    }

    public HttpRequestExceptionEx(HttpStatusCode code, string message) : this(code, message, null)
    {
    }

    public HttpRequestExceptionEx(HttpStatusCode code, string message, Exception inner) : base(message, inner)
    {
        HttpCode = code;
    }

    public HttpStatusCode HttpCode { get; }
}
