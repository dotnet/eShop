namespace eShop.ClientApp.Helpers;

public static class UriHelper
{
    private static readonly char[] _trims = {'\\', '/'};
    public static string CombineUri(params string[] uriParts)
    {
        var uri = string.Empty;
        
        if (uriParts != null && uriParts.Length > 0)
        {
            uri = (uriParts[0] ?? string.Empty).TrimEnd(_trims);
            for (var i = 1; i < uriParts.Length; i++)
            {
                uri = $"{uri.TrimEnd(_trims)}/{(uriParts[i] ?? string.Empty).TrimStart(_trims)}";
            }
        }

        return uri;
    }
}
