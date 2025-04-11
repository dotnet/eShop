namespace Inked.Identity.API.Services;

public interface IRedirectService
{
    string ExtractRedirectUriFromReturnUrl(string url);
}
