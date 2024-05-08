using System.Net;

namespace eShop.ClientApp.Services.Identity;

public class AuthorizeRequest
{
    private readonly Uri _authorizeEndpoint;

    public AuthorizeRequest(string authorizeEndpoint)
    {
        _authorizeEndpoint = new Uri(authorizeEndpoint);
    }

    public string Create(IDictionary<string, string> values)
    {
        var queryString = string.Join("&",
            values.Select(kvp =>
                string.Format("{0}={1}", WebUtility.UrlEncode(kvp.Key), WebUtility.UrlEncode(kvp.Value))).ToArray());
        return string.Format("{0}?{1}", _authorizeEndpoint.AbsoluteUri, queryString);
    }
}
