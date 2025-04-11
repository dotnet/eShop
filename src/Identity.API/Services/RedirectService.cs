using System.Net;

namespace Inked.Identity.API.Services;

public class RedirectService : IRedirectService
{
    public string ExtractRedirectUriFromReturnUrl(string url)
    {
        var decodedUrl = WebUtility.HtmlDecode(url);
        var results = Regex.Split(decodedUrl, "redirect_uri=");
        if (results.Length < 2)
        {
            return "";
        }

        var result = results[1];

        string splitKey;
        if (result.Contains("signin-oidc"))
        {
            splitKey = "signin-oidc";
        }
        else
        {
            splitKey = "scope";
        }

        results = Regex.Split(result, splitKey);
        if (results.Length < 2)
        {
            return "";
        }

        result = results[0];

        return result.Replace("%3A", ":").Replace("%2F", "/").Replace("&", "");
    }
}
