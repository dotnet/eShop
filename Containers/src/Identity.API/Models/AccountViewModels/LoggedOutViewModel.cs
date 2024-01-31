namespace eShop.Identity.API.Models.AccountViewModels
{
    public record LoggedOutViewModel
    {
        public string PostLogoutRedirectUri { get; init; }
        public string ClientName { get; init; }
        public string SignOutIframeUrl { get; init; }
    }
}
