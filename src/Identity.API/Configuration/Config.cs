namespace eShop.Identity.API.Configuration
{
    public class Config
    {
        // ApiResources define the apis in your system
        public static IEnumerable<ApiResource> GetApis()
        {
            return new List<ApiResource>
            {
                new ApiResource("orders", "Orders Service"),
                new ApiResource("basket", "Basket Service"),
                new ApiResource("webhooks", "Webhooks registration Service"),
                new ApiResource("warehouse", "Warehouse Service"),
                new ApiResource("shipping", "Shipping Service"),
            };
        }

        // ApiScope is used to protect the API 
        //The effect is the same as that of API resources in IdentityServer 3.x
        public static IEnumerable<ApiScope> GetApiScopes()
        {
            return new List<ApiScope>
            {
                new ApiScope("orders", "Orders Service"),
                new ApiScope("basket", "Basket Service"),
                new ApiScope("webhooks", "Webhooks registration Service"),
                new ApiScope("warehouse", "Warehouse Service"),
                new ApiScope("shipping", "Shipping Service"),
            };
        }

        // Identity resources are data like user ID, name, or email address of a user
        // see: http://docs.identityserver.io/en/release/configuration/resources.html
        public static IEnumerable<IdentityResource> GetResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResource("roles", "User roles", new[] { "role" })
            };
        }

        // client want to access resources (aka scopes)
        public static IEnumerable<Client> GetClients(IConfiguration configuration)
        {
            return new List<Client>
            {
                new Client
                {
                    ClientId = "maui",
                    ClientName = "eShop MAUI OpenId Client",
                    AllowedGrantTypes = GrantTypes.Code,                    
                    //Used to retrieve the access token on the back channel.
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },
                    RedirectUris = { configuration["MauiCallback"] },
                    RequireConsent = false,
                    RequirePkce = true,
                    PostLogoutRedirectUris = { $"{configuration["MauiCallback"]}/Account/Redirecting" },
                    //AllowedCorsOrigins = { "http://eshopxamarin" },
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        "orders",
                        "basket",
                        "mobileshoppingagg",
                        "webhooks"
                    },
                    //Allow requesting refresh tokens for long lived API access
                    AllowOfflineAccess = true,
                    AllowAccessTokensViaBrowser = true,
                    AlwaysIncludeUserClaimsInIdToken = true,
                    AccessTokenLifetime = 60*60*2, // 2 hours
                    IdentityTokenLifetime= 60*60*2 // 2 hours
                },
                new Client
                {
                    ClientId = "webapp",
                    ClientName = "WebApp Client",
                    ClientSecrets = new List<Secret>
                    {
                        new Secret("secret".Sha256())
                    },
                    ClientUri = $"{configuration["WebAppClient"]}",                             // public uri of the client
                    AllowedGrantTypes = GrantTypes.Code,
                    AllowAccessTokensViaBrowser = false,
                    RequireConsent = false,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = true,
                    RequirePkce = false,
                    RedirectUris = new List<string>
                    {
                        $"{configuration["WebAppClient"]}/signin-oidc"
                    },
                    PostLogoutRedirectUris = new List<string>
                    {
                        $"{configuration["WebAppClient"]}/signout-callback-oidc"
                    },
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        "orders",
                        "basket",
                        "webshoppingagg",
                        "webhooks"
                    },
                    AccessTokenLifetime = 60*60*2, // 2 hours
                    IdentityTokenLifetime= 60*60*2 // 2 hours
                },
                new Client
                {
                    ClientId = "webhooksclient",
                    ClientName = "Webhooks Client",
                    ClientSecrets = new List<Secret>
                    {
                        new Secret("secret".Sha256())
                    },
                    ClientUri = $"{configuration["WebhooksWebClient"]}",                             // public uri of the client
                    AllowedGrantTypes = GrantTypes.Code,
                    AllowAccessTokensViaBrowser = false,
                    RequireConsent = false,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = true,
                    RedirectUris = new List<string>
                    {
                        $"{configuration["WebhooksWebClient"]}/signin-oidc"
                    },
                    PostLogoutRedirectUris = new List<string>
                    {
                        $"{configuration["WebhooksWebClient"]}/signout-callback-oidc"
                    },
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        "webhooks"
                    },
                    AccessTokenLifetime = 60*60*2, // 2 hours
                    IdentityTokenLifetime= 60*60*2 // 2 hours
                },
                new Client
                {
                    ClientId = "basketswaggerui",
                    ClientName = "Basket Swagger UI",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    AllowAccessTokensViaBrowser = true,

                    RedirectUris = { $"{configuration["BasketApiClient"]}/swagger/oauth2-redirect.html" },
                    PostLogoutRedirectUris = { $"{configuration["BasketApiClient"]}/swagger/" },

                    AllowedScopes =
                    {
                        "basket"
                    }
                },
                new Client
                {
                    ClientId = "orderingswaggerui",
                    ClientName = "Ordering Swagger UI",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    AllowAccessTokensViaBrowser = true,

                    RedirectUris = { $"{configuration["OrderingApiClient"]}/swagger/oauth2-redirect.html" },
                    PostLogoutRedirectUris = { $"{configuration["OrderingApiClient"]}/swagger/" },

                    AllowedScopes =
                    {
                        "orders"
                    }
                },
                new Client
                {
                    ClientId = "webhooksswaggerui",
                    ClientName = "WebHooks Service Swagger UI",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    AllowAccessTokensViaBrowser = true,

                    RedirectUris = { $"{configuration["WebhooksApiClient"]}/swagger/oauth2-redirect.html" },
                    PostLogoutRedirectUris = { $"{configuration["WebhooksApiClient"]}/swagger/" },

                    AllowedScopes =
                    {
                        "webhooks"
                    }
                },
                new Client
                {
                    ClientId = "warehouseswaggerui",
                    ClientName = "Warehouse Service Swagger UI",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    AllowAccessTokensViaBrowser = true,

                    RedirectUris = { $"{configuration["WarehouseApiClient"] ?? "http://localhost:5000"}/swagger/oauth2-redirect.html" },
                    PostLogoutRedirectUris = { $"{configuration["WarehouseApiClient"] ?? "http://localhost:5000"}/swagger/" },

                    AllowedScopes =
                    {
                        "warehouse"
                    }
                },
                new Client
                {
                    ClientId = "admin-ui",
                    ClientName = "Admin UI",
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                    RequireClientSecret = false,
                    AllowAccessTokensViaBrowser = true,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = true,
                    AllowedCorsOrigins = new List<string>
                    {
                        configuration["AdminUiClient"] ?? "http://localhost:5173",
                        "http://localhost:5173"
                    },
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        "roles",
                        "warehouse",
                        "orders",
                        "shipping"
                    },
                    AccessTokenLifetime = 60*60*2,
                    IdentityTokenLifetime = 60*60*2
                },
                new Client
                {
                    ClientId = "shipper-ui",
                    ClientName = "Shipper UI",
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                    RequireClientSecret = false,
                    AllowAccessTokensViaBrowser = true,
                    AllowOfflineAccess = true,
                    AlwaysIncludeUserClaimsInIdToken = true,
                    AllowedCorsOrigins = new List<string>
                    {
                        configuration["ShipperUiClient"] ?? "http://localhost:5174",
                        "http://localhost:5174"
                    },
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        "roles",
                        "shipping"
                    },
                    AccessTokenLifetime = 60*60*2,
                    IdentityTokenLifetime = 60*60*2
                },
                new Client
                {
                    ClientId = "shippingswaggerui",
                    ClientName = "Shipping Service Swagger UI",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    AllowAccessTokensViaBrowser = true,

                    RedirectUris = { $"{configuration["ShippingApiClient"] ?? "http://localhost:5000"}/swagger/oauth2-redirect.html" },
                    PostLogoutRedirectUris = { $"{configuration["ShippingApiClient"] ?? "http://localhost:5000"}/swagger/" },

                    AllowedScopes =
                    {
                        "shipping"
                    }
                }
            };
        }
    }
}
